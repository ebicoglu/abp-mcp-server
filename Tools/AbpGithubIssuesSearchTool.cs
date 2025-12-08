using AbpMcpServer.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Web;

namespace AbpMcpServer.Tools;

public class AbpGithubIssuesSearchTool : IMcpTool
{
	private readonly IMemoryCache _cache;
	private readonly ILogger<AbpGithubIssuesSearchTool> _logger;
	private readonly HttpClient _httpClient;

	public AbpGithubIssuesSearchTool(IMemoryCache cache, ILogger<AbpGithubIssuesSearchTool> logger, IHttpClientFactory httpClientFactory)
	{
		_cache = cache;
		_logger = logger;
		_httpClient = httpClientFactory.CreateClient();
		_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AbpMcpServer");
	}

	public string Name => "abp.github.issues.search";
	public string Description => "Search ABP Framework GitHub issues.";
	public object InputSchema => new
	{
		type = "object",
		properties = new
		{
			query = new { type = "string", description = "Search query" },
			state = new { type = "string", description = "Issue state (open/closed)", @default = "open" }
		},
		required = new[] { "query" }
	};

	public async Task<object> ExecuteAsync(Dictionary<string, object> arguments)
	{
		var query = arguments["query"].ToString();
		var state = arguments.ContainsKey("state") ? arguments["state"].ToString() : "open";
		var cacheKey = $"gh_issues_{state}_{query}";

		if (_cache.TryGetValue(cacheKey, out List<SearchResultItem>? cachedResults))
		{
			return new { items = cachedResults };
		}

		_logger.LogInformation(">>> Searching GitHub issues for '{query}'...", query);

		var q = HttpUtility.UrlEncode($"repo:abpframework/abp {query} state:{state}");
		var url = $"https://api.github.com/search/issues?q={q}&per_page=5";

		var response = await _httpClient.GetAsync(url);
		response.EnsureSuccessStatusCode();

		var json = await response.Content.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(json);
		var items = doc.RootElement.GetProperty("items");
		var sbLog = new StringBuilder();

		var results = new List<SearchResultItem>();
		foreach (var item in items.EnumerateArray())
		{
			results.Add(new SearchResultItem
			{
				Title = item.GetProperty("title").GetString() ?? "",
				Url = item.GetProperty("html_url").GetString() ?? "",
				Snippet = item.GetProperty("body").GetString()?.Substring(0, Math.Min(item.GetProperty("body").GetString()?.Length ?? 0, 200)) + "..." ?? ""
			});

			sbLog.AppendLine(item.GetProperty("title").GetString() + " - " + item.GetProperty("html_url").GetString());
		}

		_cache.Set(cacheKey, results, TimeSpan.FromMinutes(60));

		_logger.LogInformation("<<< Searching GitHub issues completed. Results: ({resultCount}):\n\r{results}", results.Count, sbLog.ToString());

		return new { items = results };
	}
}
