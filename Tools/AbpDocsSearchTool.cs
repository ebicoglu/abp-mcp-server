using AbpMcpServer.Models;
using AngleSharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AbpMcpServer.Tools;

public class AbpDocsSearchTool : IMcpTool
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AbpDocsSearchTool> _logger;
    private readonly HttpClient _httpClient;

    public AbpDocsSearchTool(IMemoryCache cache, ILogger<AbpDocsSearchTool> logger, IHttpClientFactory httpClientFactory)
    {
        _cache = cache;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public string Name => "abp.docs.search";
    public string Description => "Search ABP Framework documentation.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            query = new { type = "string", description = "Search query" },
            version = new { type = "string", description = "Documentation version (e.g., 'latest', '9.1')", @default = "latest" }
        },
        required = new[] { "query" }
    };

    public async Task<object> ExecuteAsync(Dictionary<string, object> arguments)
    {
        var query = arguments["query"].ToString();
        var version = arguments.ContainsKey("version") ? arguments["version"].ToString() : "latest";
        var cacheKey = $"docs_{version}_{query}";

        if (_cache.TryGetValue(cacheKey, out List<SearchResultItem>? cachedResults))
        {
            return new { items = cachedResults };
        }

        // Since ABP docs don't have a public search API we can easily hit without auth or complex logic,
        // and scraping Google results is flaky, we will try to scrape the search page if it exists,
        // or for this MVP, we might have to rely on a known search endpoint or just scrape the main page structure if possible.
        // However, ABP docs use Algolia usually. Let's check if we can use a simple site:abp.io search or similar.
        // For now, let's implement a basic scraper that assumes we can search or list pages.

        // Actually, a better approach for "Search" without an API is often using Google Custom Search or similar, but we want to be self-contained.
        // Let's try to hit the docs search URL if we can find one.
        // Looking at abp.io/docs, it uses Algolia.
        // Reverse engineering Algolia keys is possible but might be brittle.

        // Alternative: Scrape the navigation tree to find matching titles.
        // The navigation tree is usually loaded on the docs page.

        var url = $"https://abp.io/docs/{version}";
        var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
        var document = await context.OpenAsync(url);

        var results = new List<SearchResultItem>();

        // This is a naive implementation: it looks for links in the nav menu that match the query.
        // A real search would need the Algolia index or a server-side search handler.
        var links = document.QuerySelectorAll("a");

        foreach (var link in links)
        {
            var title = link.TextContent.Trim();
            var href = link.GetAttribute("href");

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(href) &&
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
                    Snippet = "Found in documentation navigation."
                });

                if (results.Count >= 5) break;
            }
        }

        _cache.Set(cacheKey, results, TimeSpan.FromMinutes(10));

        return new { items = results };
    }
}
