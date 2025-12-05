using AbpMcpServer.Models;

namespace AbpMcpServer.Tools;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    object InputSchema { get; }
    Task<object> ExecuteAsync(Dictionary<string, object> arguments);
}
