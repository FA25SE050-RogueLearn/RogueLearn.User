using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;

namespace RogueLearn.User.Application.Plugins
{
    /// <summary>
    /// Google web search optimized for finding FREE, high-quality developer tutorials and community blogs.
    /// Prioritizes community sites over official documentation.
    /// </summary>
    public class GoogleWebSearchService : IWebSearchService
    {
        private readonly string _apiKey;
        private readonly string _searchEngineId;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleWebSearchService>? _logger;

        private static readonly string[] BlockedSources = new[]
        {
            "medium.com", // Often paywalled
            "towardsdatascience.com", // Paywalled
        };

        private static readonly string[] TutorialSites = new[]
        {
            // International
            "geeksforgeeks.org",
            "w3schools.com",
            "tutorialspoint.com",
            "programiz.com",
            "javatpoint.com",
            "tutorialsteacher.com",
            
            // Vietnamese tutorial sites
            "viblo.asia",
            "topdev.vn",
            "200lab.io",
        };

        // HIGH PRIORITY: Community blogs and developer platforms
        private static readonly string[] CommunityBlogs = new[]
        {
            // Major community platforms
            "dev.to",
            "hashnode.dev",
            "freecodecamp.org",
            "digitalocean.com/community",
            "css-tricks.com",
            "smashingmagazine.com",
            "logrocket.com/blog",
            "scotch.io",
            
            // Personal developer blogs (highly recommended on Reddit)
            "kentcdodds.com",
            "joshwcomeau.com",
            "overreacted.io",
            "dan.luu",
            "pragmaticengineer.com",
            "martinfowler.com",
            "joelonsoftware.com",
            "blog.codinghorror.com",
            
            // Tech company engineering blogs
            "engineering.fb.com",
            "netflixtechblog.com",
            "eng.uber.com",
            "dropbox.tech",
            
            // Vietnamese community
            "techtalk.vn",
            "techmaster.vn",
        };

        // MEDIUM PRIORITY: Official docs (only as reference, not primary)
        private static readonly string[] OfficialDocs = new[]
        {
            "developer.mozilla.org",
            "web.dev",
            "developer.android.com",
            "learn.microsoft.com",
            "react.dev",
            "vuejs.org",
            "angular.io",
            "docs.oracle.com",
            "spring.io",
            "baeldung.com",
        };

        public GoogleWebSearchService(
            string apiKey,
            string searchEngineId,
            HttpClient httpClient,
            ILogger<GoogleWebSearchService>? logger = null)
        {
            _apiKey = apiKey;
            _searchEngineId = searchEngineId;
            _httpClient = httpClient;
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
                var enhancedQuery = EnhanceSearchQuery(query);
                _logger?.LogDebug("Enhanced search query: '{EnhancedQuery}' (original: '{Query}')",
                    enhancedQuery, query);

                var encodedQuery = HttpUtility.UrlEncode(enhancedQuery);
                var url = $"https://www.googleapis.com/customsearch/v1" +
                         $"?key={_apiKey}" +
                         $"&cx={_searchEngineId}" +
                         $"&q={encodedQuery}" +
                         $"&num={Math.Min(count, 10)}" +
                         $"&start={offset + 1}";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var searchResults = JsonSerializer.Deserialize<GoogleSearchResponse>(jsonResponse);

                if (searchResults?.Items == null || !searchResults.Items.Any())
                {
                    _logger?.LogWarning("No search results for query: '{Query}'", query);
                    return Enumerable.Empty<string>();
                }

                // Sort results by source priority
                var prioritizedResults = PrioritizeResults(searchResults.Items);

                return prioritizedResults.Select(item =>
                {
                    var source = GetSourceType(item.Link);
                    return $"Title: {item.Title}\n" +
                           $"Link: {item.Link}\n" +
                           $"Snippet: {item.Snippet}\n" +
                           $"Source: {source}";
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Web search failed for query: '{Query}'", query);
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Enhance query to prioritize community blogs and tutorial sites.
        /// </summary>
        private string EnhanceSearchQuery(string query)
        {
            var queryLower = query.ToLowerInvariant();
            var enhancedQuery = new StringBuilder(query);

            var contentKeywords = new[] {
                "tutorial", "guide", "documentation", "example",
                "introduction", "beginner", "learn", "explained"
            };
            bool hasContentKeyword = contentKeywords.Any(k => queryLower.Contains(k));

            if (!hasContentKeyword)
            {
                enhancedQuery.Append(" tutorial guide");
            }

            if (IsVietnameseQuery(query))
            {
                _logger?.LogDebug("🇻🇳 Detected Vietnamese query, boosting Vietnamese sources");

                var vietnameseSites = string.Join(" OR ", new[]
                {
                    "site:viblo.asia",
                    "site:topdev.vn",
                    "site:techtalk.vn",
                    "site:200lab.io"
                });

                enhancedQuery.Append($" ({vietnameseSites})");
            }
            else
            {
                var seBoost = LooksLikeRequirements(query)
                    ? " OR visual-paradigm.com OR atlassian.com OR lucidchart.com OR smartsheet.com OR productplan.com OR geeksforgeeks.org/software-engineering"
                    : string.Empty;
                enhancedQuery.Append($" (geeksforgeeks OR dev.to OR freecodecamp OR w3schools OR programiz{seBoost})");
            }

            return enhancedQuery.ToString();
        }

        private bool IsVietnameseQuery(string query)
        {
            return Regex.IsMatch(query, @"[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđĐ]");
        }

        private bool LooksLikeRequirements(string query)
        {
            var q = query.ToLowerInvariant();
            return q.Contains("requirement") || q.Contains("requirements") || q.Contains("use case") || q.Contains("acceptance criteria") || q.Contains("elicitation") || q.Contains("validation") || q.Contains("vision and scope");
        }

        private List<GoogleSearchItem> PrioritizeResults(List<GoogleSearchItem> items)
        {
            var prioritized = items.OrderByDescending(item =>
            {
                var link = item.Link.ToLowerInvariant();

                if (TutorialSites.Any(site => link.Contains(site)))
                    return 1000;

                if (CommunityBlogs.Any(site => link.Contains(site)))
                    return 900;

                if (OfficialDocs.Any(site => link.Contains(site)))
                    return 500;

                if (link.Contains("stackoverflow.com"))
                    return 400;

                if (link.Contains("reddit.com"))
                    return 300;

                return 100;
            }).ToList();

            _logger?.LogDebug("Prioritized {Count} results. Top source: {TopSource}",
                prioritized.Count,
                prioritized.FirstOrDefault()?.Link);

            return prioritized;
        }

        /// <summary>
        /// Classify source type for metadata.
        /// </summary>
        private string GetSourceType(string url)
        {
            var urlLower = url.ToLowerInvariant();

            if (TutorialSites.Any(site => urlLower.Contains(site)))
                return "Trusted Tutorial Site ⭐";

            if (CommunityBlogs.Any(site => urlLower.Contains(site)))
                return "Trusted Community Blog ⭐";

            if (OfficialDocs.Any(site => urlLower.Contains(site)))
                return "Official Documentation";

            if (urlLower.Contains("stackoverflow.com"))
                return "Stack Overflow Q&A";

            if (urlLower.Contains("github.com"))
                return "GitHub Repository";

            if (urlLower.Contains("reddit.com"))
                return "Reddit Discussion";

            return "General Source";
        }

        private class GoogleSearchResponse
        {
            [JsonPropertyName("items")]
            public List<GoogleSearchItem>? Items { get; set; }
        }

        private class GoogleSearchItem
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("link")]
            public string Link { get; set; } = string.Empty;

            [JsonPropertyName("snippet")]
            public string Snippet { get; set; } = string.Empty;
        }
    }
}
