using RogueLearn.User.Application.Interfaces;
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
        private readonly HttpClient _httpClient = new HttpClient();

        public GoogleWebSearchService(string apiKey, string searchEngineId)
        {
            _apiKey = apiKey;
            _searchEngineId = searchEngineId;
        }

        public async Task<IEnumerable<string>> SearchAsync(
            string query,
            int count = 10,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            // Google's Custom Search JSON API endpoint
            // https://www.googleapis.com/customsearch/v1?key=...&cx=...&q=...&start=...&num=...
            var baseUrl = "https://www.googleapis.com/customsearch/v1";
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["key"] = _apiKey;
            queryString["cx"] = _searchEngineId;
            queryString["q"] = query;
            queryString["num"] = Math.Clamp(count, 1, 10).ToString();
            queryString["start"] = (offset + 1).ToString(); // 1-based index

            var url = $"{baseUrl}?{queryString}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

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

                    var resultBuilder = new StringBuilder();
                    resultBuilder.AppendLine($"Title: {title}");
                    resultBuilder.AppendLine($"Link: {link}");
                    resultBuilder.AppendLine($"Snippet: {snippet}");
                    results.Add(resultBuilder.ToString());
                }
            }

            return results;
        }
    }
}