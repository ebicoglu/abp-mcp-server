using AbpMcpServer.Services;
using AbpMcpServer.Tools;
using AbpMcpServer.Services.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AbpMcpServer;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var server = serviceProvider.GetRequiredService<McpServer>();

        //for test using `test_input.json`
        await server.RunAsync(File.OpenRead("test_input.json"));
        
        //for production using StdIn/StdOut
        //await server.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(configure =>
        {
            // We must use Stderr for logging because Stdout is used for the MCP protocol
            configure.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
            configure.AddFile(options =>
            {
                options.Params.LogPath = "logs/mcp_server.log";
            });
            configure.SetMinimumLevel(LogLevel.Debug);
        });

        // Caching
        services.AddMemoryCache();
        services.AddHttpClient();

        // Tools
        services.AddSingleton<IMcpTool, AbpDocsSearchTool>();
        services.AddSingleton<IMcpTool, AbpGithubIssuesSearchTool>();
        services.AddSingleton<IMcpTool, AbpSupportQuestionsSearchTool>();
        services.AddSingleton<IMcpTool, AbpCommunityArticlesSearchTool>();

        // Core
        services.AddSingleton<McpRouter>();
        services.AddSingleton<McpServer>();
    }
}
