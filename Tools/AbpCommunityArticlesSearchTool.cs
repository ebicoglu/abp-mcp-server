using System.Text;
using AbpMcpServer.Models;
using AngleSharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AbpMcpServer.Tools;

public class AbpCommunityArticlesSearchTool : IMcpTool
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AbpCommunityArticlesSearchTool> _logger;
    private readonly HttpClient _httpClient;

    public AbpCommunityArticlesSearchTool(IMemoryCache cache, ILogger<AbpCommunityArticlesSearchTool> logger, IHttpClientFactory httpClientFactory)
    {
        _cache = cache;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public string Name => "abp.community.articles.search";
    public string Description => "Search ABP Community articles.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            query = new { type = "string", description = "Search query" }
        },
        required = new[] { "query" }
    };

    public async Task<object> ExecuteAsync(Dictionary<string, object> arguments)
    {
        var query = arguments["query"].ToString();
        var cacheKey = $"articles_{query}";

        if (_cache.TryGetValue(cacheKey, out List<SearchResultItem>? cachedResults))
        {
            return new { items = cachedResults };
        }

        _logger.LogInformation("*** Searching ABP Community Articles for '{query}'...", query);

        var url = "https://abp.io/community/articles";
        var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
        var document = await context.OpenAsync(url);

        var results = new List<SearchResultItem>();
        var links = document.QuerySelectorAll("a");
        var sbLog = new StringBuilder();

        foreach (var link in links)
        {
            var title = link.TextContent.Trim();
            var href = link.GetAttribute("href");

            if (!string.IsNullOrWhiteSpace(title) && 
                !string.IsNullOrWhiteSpace(href) &&
                title.Contains(query!, StringComparison.OrdinalIgnoreCase))
            {
                if (!href.StartsWith("http"))
                {
                    href = new Uri(new Uri("https://abp.io"), href).ToString();
                }

                results.Add(new SearchResultItem
                {
                    Title = title,
                    Url = href,
                    Snippet = "Found in community articles."
                });

                sbLog.AppendLine(title + " - " + href);
                if (results.Count >= 5) break;
            }
        }

        _cache.Set(cacheKey, results, TimeSpan.FromMinutes(10));

        _logger.LogInformation("*** Searching ABP Community Articles completed. Results: {results}", sbLog);

        return new { items = results };
    }
}
