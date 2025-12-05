using System.Text.Json;
using AbpMcpServer.Models;
using Microsoft.Extensions.Logging;

namespace AbpMcpServer.Services;

public class McpServer
{
    private readonly ILogger<McpServer> _logger;
    private readonly McpRouter _router;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServer(ILogger<McpServer> logger, McpRouter router)
    {
        _logger = logger;
        _router = router;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ABP MCP Server started...");

        using var stdin = Console.OpenStandardInput();
        using var reader = new StreamReader(stdin);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;

                if (string.IsNullOrWhiteSpace(line)) continue;

                _logger.LogDebug("Received: {Line}", line);

                JsonRpcRequest? request = null;
                try
                {
                    request = JsonSerializer.Deserialize<JsonRpcRequest>(line, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse JSON-RPC request");
                    continue;
                }

                if (request == null) continue;

                var response = await HandleRequestAsync(request);
                if (response != null)
                {
                    var json = JsonSerializer.Serialize(response, _jsonOptions);
                    Console.WriteLine(json);
                    _logger.LogDebug("Sent: {Json}", json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
            }
        }
    }

    private async Task<JsonRpcResponse?> HandleRequestAsync(JsonRpcRequest request)
    {
        try
        {
            object? result = null;

            switch (request.Method)
            {
                case "initialize":
                    result = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = new
                        {
                            tools = new Dictionary<string, object>()
                        },
                        serverInfo = new
                        {
                            name = "abp-mcp-server",
                            version = "1.0.0"
                        }
                    };
                    break;
                case "tools/list":
                    result = new
                    {
                        tools = _router.GetTools()
                    };
                    break;
                case "tools/call":
                    if (request.Params is JsonElement paramsElement)
                    {
                        var callParams = JsonSerializer.Deserialize<CallToolParams>(paramsElement.GetRawText(), _jsonOptions);
                        if (callParams != null)
                        {
                            result = await _router.CallToolAsync(callParams.Name, callParams.Arguments ?? new Dictionary<string, object>());
                        }
                    }
                    break;
                default:
                    // Ignore unknown notifications, return error for requests
                    if (request.Id != null)
                    {
                        return new JsonRpcResponse
                        {
                            JsonRpc = "2.0",
                            Id = request.Id,
                            Error = new JsonRpcError { Code = -32601, Message = "Method not found" }
                        };
                    }
                    return null;
            }

            return new JsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling method {Method}", request.Method);
            return new JsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new JsonRpcError { Code = -32000, Message = ex.Message }
            };
        }
    }
}
