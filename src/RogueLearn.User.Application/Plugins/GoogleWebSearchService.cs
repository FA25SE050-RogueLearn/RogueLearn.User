using RogueLearn.User.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Web;

namespace RogueLearn.User.Application.Plugins
{
    /// <summary>
    /// Google web search service that returns formatted search results strings.
    /// </summary>
    public class GoogleWebSearchService : IWebSearchService
    {
        private readonly string _apiKey;
        private readonly string _searchEngineId;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleWebSearchService>? _logger;

        public GoogleWebSearchService(
            string apiKey,
            string searchEngineId,
            IHttpClientFactory? httpClientFactory = null,
            ILogger<GoogleWebSearchService>? logger = null)
        {
            _apiKey = apiKey;
            _searchEngineId = searchEngineId;
            _httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();
            _logger = logger;
        }

        public async Task<IEnumerable<string>> SearchAsync(
            string query,
            int count = 10,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Enhance query for better documentation results
                var enhancedQuery = EnhanceSearchQuery(query);

                _logger?.LogDebug("Original query: '{OriginalQuery}', Enhanced query: '{EnhancedQuery}'", query, enhancedQuery);

                // Google's Custom Search JSON API endpoint
                var baseUrl = "https://www.googleapis.com/customsearch/v1";
                var queryString = HttpUtility.ParseQueryString(string.Empty);
                queryString["key"] = _apiKey;
                queryString["cx"] = _searchEngineId;
                queryString["q"] = enhancedQuery;
                queryString["num"] = Math.Clamp(count, 1, 10).ToString();
                queryString["start"] = (offset + 1).ToString(); // 1-based index

                var url = $"{baseUrl}?{queryString}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogError("Google Custom Search API error: {StatusCode} - {ErrorContent}",
                        response.StatusCode, errorContent);
                    return Enumerable.Empty<string>();
                }

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

                var results = new List<string>();

                if (json.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var title = item.TryGetProperty("title", out var t) ? t.GetString() : string.Empty;
                        var link = item.TryGetProperty("link", out var l) ? l.GetString() : string.Empty;
                        var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() : string.Empty;

                        // Skip results without links
                        if (string.IsNullOrWhiteSpace(link))
                        {
                            continue;
                        }

                        var resultBuilder = new StringBuilder();
                        resultBuilder.AppendLine($"Title: {title}");
                        resultBuilder.AppendLine($"Link: {link}");
                        resultBuilder.AppendLine($"Snippet: {snippet}");
                        results.Add(resultBuilder.ToString());
                    }

                    _logger?.LogInformation("Google Custom Search returned {Count} results for query '{Query}'",
                        results.Count, query);
                }
                else
                {
                    _logger?.LogWarning("Google Custom Search returned no items for query '{Query}'", query);
                }

                return results;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP error during Google Custom Search for query '{Query}'", query);
                return Enumerable.Empty<string>();
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "JSON parsing error during Google Custom Search for query '{Query}'", query);
                return Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during Google Custom Search for query '{Query}'", query);
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Enhances search queries to prioritize official documentation and tutorials
        /// </summary>
        private string EnhanceSearchQuery(string query)
        {
            var queryLower = query.ToLowerInvariant();

            // For Android topics, prioritize developer.android.com
            if (queryLower.Contains("android"))
            {
                return $"{query} site:developer.android.com OR site:developer.android.com/guide";
            }

            // For ASP.NET topics, prioritize Microsoft Learn
            if (queryLower.Contains("asp.net") || queryLower.Contains("aspnet") || queryLower.Contains(".net"))
            {
                return $"{query} site:learn.microsoft.com OR site:docs.microsoft.com";
            }

            // For Java/Spring topics
            if (queryLower.Contains("spring boot") || queryLower.Contains("spring"))
            {
                return $"{query} site:spring.io";
            }

            if (queryLower.Contains("java") && !queryLower.Contains("javascript"))
            {
                return $"{query} site:docs.oracle.com OR site:docs.oracle.com/en/java";
            }

            // For Python topics
            if (queryLower.Contains("django"))
            {
                return $"{query} site:docs.djangoproject.com";
            }

            if (queryLower.Contains("flask"))
            {
                return $"{query} site:flask.palletsprojects.com";
            }

            if (queryLower.Contains("python"))
            {
                return $"{query} site:docs.python.org OR tutorial";
            }

            // For JavaScript frameworks
            if (queryLower.Contains("react"))
            {
                return $"{query} site:react.dev OR site:reactjs.org";
            }

            if (queryLower.Contains("vue"))
            {
                return $"{query} site:vuejs.org";
            }

            if (queryLower.Contains("angular"))
            {
                return $"{query} site:angular.io";
            }

            // Default: Add "official documentation" or "tutorial" to improve results
            if (queryLower.Contains("tutorial") || queryLower.Contains("guide") || queryLower.Contains("documentation"))
            {
                return query; // Already has good keywords
            }

            return $"{query} official documentation tutorial";
        }
    }
}