using AbpMcpServer.Models;
using AbpMcpServer.Tools;

namespace AbpMcpServer.Services;

public class McpRouter
{
    private readonly Dictionary<string, IMcpTool> _tools = new();

    public McpRouter(IEnumerable<IMcpTool> tools)
    {
        foreach (var tool in tools)
        {
            _tools[tool.Name] = tool;
        }
    }

    public List<McpTool> GetTools()
    {
        return _tools.Values.Select(t => new McpTool
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.InputSchema
        }).ToList();
    }

    public async Task<object> CallToolAsync(string name, Dictionary<string, object> arguments)
    {
        if (_tools.TryGetValue(name, out var tool))
        {
            return await tool.ExecuteAsync(arguments);
        }
        throw new Exception($"Tool '{name}' not found.");
    }
}
