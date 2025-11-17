// RogueLearn.User/src/RogueLearn.User.Application/Services/ReadingUrlService.cs
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Implements the logic for sourcing valid, accessible URLs for reading materials.
/// </summary>
public class ReadingUrlService : IReadingUrlService
{
    private readonly IWebSearchService _webSearchService;
    private readonly IUrlValidationService _urlValidationService;
    private readonly ILogger<ReadingUrlService> _logger;

    public ReadingUrlService(
        IWebSearchService webSearchService,
        IUrlValidationService urlValidationService,
        ILogger<ReadingUrlService> logger)
    {
        _webSearchService = webSearchService;
        _urlValidationService = urlValidationService;
        _logger = logger;
    }

    public async Task<string?> GetValidUrlForTopicAsync(string topic, IEnumerable<string> readings, CancellationToken cancellationToken = default)
    {
        // Handle null/empty readings
        var readingsList = readings?.ToList() ?? new List<string>();

        // 1. Prioritize finding a valid, accessible URL directly within the provided reading materials.
        _logger.LogDebug("Checking {Count} readings for valid URLs for topic '{Topic}'", readingsList.Count, topic);

        foreach (var reading in readingsList)
        {
            if (string.IsNullOrWhiteSpace(reading))
            {
                continue;
            }

            if (Uri.TryCreate(reading, UriKind.Absolute, out var uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                _logger.LogDebug("Found URL in reading material: '{Url}'", reading);

                // Validate that the URL is actually accessible and not a soft 404
                if (await _urlValidationService.IsUrlAccessibleAsync(reading, cancellationToken))
                {
                    _logger.LogInformation("Found and validated URL '{Url}' in syllabus readings for topic '{Topic}'.", reading, topic);
                    return reading;
                }

                _logger.LogWarning("URL '{Url}' in syllabus is not accessible or returns error page for topic '{Topic}'.", reading, topic);
            }
        }

        // 2. Try to get a fallback URL based on common patterns in the topic
        var fallbackUrl = GetFallbackUrlForTopic(topic);
        if (!string.IsNullOrWhiteSpace(fallbackUrl))
        {
            _logger.LogInformation("Using fallback URL '{Url}' for topic '{Topic}'", fallbackUrl, topic);

            // Validate the fallback URL
            if (await _urlValidationService.IsUrlAccessibleAsync(fallbackUrl, cancellationToken))
            {
                _logger.LogInformation("Validated fallback URL '{Url}' for topic '{Topic}'.", fallbackUrl, topic);
                return fallbackUrl;
            }

            _logger.LogWarning("Fallback URL '{Url}' is not accessible for topic '{Topic}'.", fallbackUrl, topic);
        }

        // 3. If no valid URL is found, fall back to the web search service.
        _logger.LogInformation("No valid URL in syllabus or fallbacks for topic '{Topic}'. Falling back to web search.", topic);

        try
        {
            // Create a better search query by combining topic with technology keywords
            var searchQuery = $"{topic} tutorial guide documentation";
            _logger.LogDebug("Searching web with query: '{Query}'", searchQuery);

            // Request multiple results to have fallback options
            var searchResults = await _webSearchService.SearchAsync(searchQuery, count: 5, offset: 0, cancellationToken);

            if (searchResults == null || !searchResults.Any())
            {
                _logger.LogWarning("Web search returned no results for topic '{Topic}'", topic);
                return null;
            }

            _logger.LogDebug("Web search returned {Count} results for topic '{Topic}'", searchResults.Count(), topic);

            foreach (var result in searchResults)
            {
                // The search result is a formatted string; extract the URL
                var lines = result.Split('\n');
                var urlLine = lines.FirstOrDefault(line => line.StartsWith("Link: ", StringComparison.OrdinalIgnoreCase));

                if (urlLine != null)
                {
                    var url = urlLine.Substring("Link: ".Length).Trim();
                    _logger.LogDebug("Extracted URL from search result: '{Url}'", url);

                    // Validate each search result URL before accepting it
                    if (await _urlValidationService.IsUrlAccessibleAsync(url, cancellationToken))
                    {
                        _logger.LogInformation("Web search found and validated URL '{Url}' for topic '{Topic}'.", url, topic);
                        return url;
                    }

                    _logger.LogDebug("Web search URL '{Url}' is not accessible for topic '{Topic}'.", url, topic);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web search failed for topic '{Topic}'.", topic);
        }

        // 4. If all else fails, return null.
        _logger.LogWarning("Could not source a valid, accessible URL for topic '{Topic}' after checking syllabus, fallbacks, and web search.", topic);
        return null;
    }

    private string? GetFallbackUrlForTopic(string topic)
    {
        var topicLower = topic.ToLowerInvariant();

        // Android topics
        if (topicLower.Contains("android"))
        {
            if (topicLower.Contains("studio")) return "https://developer.android.com/studio/intro";
            if (topicLower.Contains("activity") || topicLower.Contains("activities")) return "https://developer.android.com/guide/components/activities/intro-activities";
            if (topicLower.Contains("layout") || topicLower.Contains("ui") || topicLower.Contains("constraint")) return "https://developer.android.com/guide/topics/ui/declaring-layout";
            if (topicLower.Contains("intent")) return "https://developer.android.com/guide/components/intents-filters";
            if (topicLower.Contains("lifecycle")) return "https://developer.android.com/guide/components/activities/activity-lifecycle";
            if (topicLower.Contains("fragment")) return "https://developer.android.com/guide/fragments";
            if (topicLower.Contains("recyclerview") || topicLower.Contains("list")) return "https://developer.android.com/guide/topics/ui/layout/recyclerview";
            if (topicLower.Contains("navigation")) return "https://developer.android.com/guide/navigation";
            if (topicLower.Contains("viewmodel") || topicLower.Contains("livedata")) return "https://developer.android.com/topic/libraries/architecture/viewmodel";
            if (topicLower.Contains("room") || topicLower.Contains("database")) return "https://developer.android.com/training/data-storage/room";
            if (topicLower.Contains("retrofit") || topicLower.Contains("networking")) return "https://square.github.io/retrofit/";

            // Generic Android fallback
            return "https://developer.android.com/guide";
        }

        // ASP.NET Core topics
        if (topicLower.Contains("asp.net") || topicLower.Contains("aspnet"))
        {
            if (topicLower.Contains("mvc")) return "https://learn.microsoft.com/en-us/aspnet/core/mvc/overview";
            if (topicLower.Contains("razor")) return "https://learn.microsoft.com/en-us/aspnet/core/razor-pages/";
            if (topicLower.Contains("api") || topicLower.Contains("web api")) return "https://learn.microsoft.com/en-us/aspnet/core/web-api/";
            if (topicLower.Contains("dependency injection") || topicLower.Contains("di")) return "https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection";
            if (topicLower.Contains("entity framework") || topicLower.Contains("ef core")) return "https://learn.microsoft.com/en-us/ef/core/";
            if (topicLower.Contains("middleware")) return "https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/";
            if (topicLower.Contains("authentication") || topicLower.Contains("identity")) return "https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity";

            // Generic ASP.NET Core fallback
            return "https://learn.microsoft.com/en-us/aspnet/core/";
        }

        // Java topics
        if (topicLower.Contains("java") && !topicLower.Contains("javascript"))
        {
            if (topicLower.Contains("spring boot")) return "https://spring.io/guides/gs/spring-boot/";
            if (topicLower.Contains("spring")) return "https://spring.io/guides";
            if (topicLower.Contains("jpa") || topicLower.Contains("hibernate")) return "https://spring.io/guides/gs/accessing-data-jpa/";

            return "https://docs.oracle.com/en/java/";
        }

        // Python topics
        if (topicLower.Contains("python"))
        {
            if (topicLower.Contains("django")) return "https://docs.djangoproject.com/en/stable/intro/tutorial01/";
            if (topicLower.Contains("flask")) return "https://flask.palletsprojects.com/en/latest/quickstart/";
            if (topicLower.Contains("fastapi")) return "https://fastapi.tiangolo.com/tutorial/";

            return "https://docs.python.org/3/tutorial/";
        }

        // React topics
        if (topicLower.Contains("react"))
        {
            if (topicLower.Contains("hook")) return "https://react.dev/learn/hooks-intro";
            if (topicLower.Contains("component")) return "https://react.dev/learn/your-first-component";
            if (topicLower.Contains("state")) return "https://react.dev/learn/state-a-components-memory";

            return "https://react.dev/learn";
        }

        // No fallback found
        return null;
    }
}