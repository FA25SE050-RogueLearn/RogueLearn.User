// RogueLearn.User/src/RogueLearn.User.Application/Services/ReadingUrlService.cs

using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Sources valid, accessible URLs with CONTEXT-AWARE and CATEGORY-AWARE relevance checking.
/// Now integrates with per-session AI-generated queries to guarantee diverse results.
/// 
/// RACE CONDITION FIX: Uses ConcurrentDictionary + live check delegate for thread-safe URL uniqueness.
/// 
/// FLOW:
/// 1. Receives topic + pre-generated AI queries (from batch generation)
/// 2. Receives live URL check delegate from handler (concurrent safety)
/// 3. Searches with SPECIFIC queries instead of generic ones
/// 4. Different queries for each session = Different URLs (no duplicates!)
/// </summary>
public class ReadingUrlService : IReadingUrlService
{
    private readonly IWebSearchService _webSearchService;
    private readonly IUrlValidationService _urlValidationService;
    private readonly IAiQueryClassificationService _aiQueryService;
    private readonly ILogger<ReadingUrlService> _logger;

    public ReadingUrlService(
        IWebSearchService webSearchService,
        IUrlValidationService urlValidationService,
        IAiQueryClassificationService aiQueryService,
        ILogger<ReadingUrlService> logger)
    {
        _webSearchService = webSearchService;
        _urlValidationService = urlValidationService;
        _aiQueryService = aiQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Main entry point: Get valid URL for a single session's topic.
    /// NOW: Accepts pre-generated specific queries that guarantee diversity.
    /// NOW: Accepts live URL check delegate for concurrent thread safety.
    /// </summary>
    public async Task<string?> GetValidUrlForTopicAsync(
        string topic,
        IEnumerable<string> readings,
        string? subjectContext = null,
        SubjectCategory category = SubjectCategory.General,
        List<string>? overrideQueries = null,  // ✅ KEY: Pre-generated AI queries from batch
        Func<string, bool>? isUrlUsedCheck = null, // ✅ KEY: Live uniqueness check (thread-safe)
        CancellationToken cancellationToken = default)
    {
        var readingsList = readings?.ToList() ?? new List<string>();

        _logger.LogInformation(
            "🔍 [Session Search] Topic: '{Topic}' | Context: '{Context}' | Category: {Category}",
            topic, subjectContext ?? "none", category);

        // Extract technology keywords from context
        var technologyKeywords = ExtractTechnologyKeywords(subjectContext);
        _logger.LogDebug("Detected technologies: {Technologies}",
            string.Join(", ", technologyKeywords));

        // ============================================================================
        // TIER 1: Check syllabus readings first
        // ============================================================================
        _logger.LogDebug("[TIER 1] Checking {Count} existing syllabus readings...", readingsList.Count);

        foreach (var reading in readingsList)
        {
            if (string.IsNullOrWhiteSpace(reading)) continue;

            // ⭐ CRITICAL FIX: Just-in-time duplicate check via live delegate
            if (isUrlUsedCheck != null && isUrlUsedCheck(reading)) continue;

            if (Uri.TryCreate(reading, UriKind.Absolute, out var uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                _logger.LogDebug("[TIER 1] Validating syllabus URL: '{Url}'", reading);

                if (await _urlValidationService.IsUrlAccessibleAsync(reading, cancellationToken))
                {
                    var score = CalculateRelevanceScore(reading, $"Link: {reading}", topic,
                        technologyKeywords, category);

                    if (score > 0)
                    {
                        _logger.LogInformation("✅ [TIER 1] Using syllabus URL (relevant, score: {Score}): {Url}",
                            score, reading);
                        return reading;
                    }

                    _logger.LogWarning("⚠️ [TIER 1] Syllabus URL failed relevance gate (score: {Score}): {Url}",
                        score, reading);
                }
            }
        }

        // ============================================================================
        // TIER 2: Web search with specific, AI-generated queries
        // ============================================================================
        _logger.LogInformation("[TIER 2] Searching with AI-generated queries to find diverse URLs...");

        try
        {
            List<string> queryVariants;

            // ✅ KEY DIFFERENCE: Use pre-provided queries (from batch generation)
            if (overrideQueries != null && overrideQueries.Any())
            {
                queryVariants = overrideQueries;
                _logger.LogInformation(
                    "📋 Using {Count} PRE-GENERATED AI queries (session-specific, diverse)",
                    queryVariants.Count);
                _logger.LogDebug("Queries: {Queries}", string.Join(" | ", queryVariants.Take(2)));
            }
            else
            {
                // Fallback: Generate inline (if no pre-generated queries provided)
                _logger.LogWarning("⚠️ No pre-generated queries provided. Generating inline...");
                queryVariants = await GenerateSearchQueriesWithLLM(topic, subjectContext, category, cancellationToken);

                if (!queryVariants.Any())
                {
                    _logger.LogWarning("LLM query generation failed, falling back to rule-based queries");
                    queryVariants = BuildQueryVariants(topic, subjectContext, category);
                }
            }

            _logger.LogDebug("Final query set: {Variants}", string.Join(" || ", queryVariants.Take(2)));

            // Execute searches with each query variant
            var aggregatedResults = new List<string>();
            int queryIndex = 0;

            foreach (var variant in queryVariants)
            {
                queryIndex++;
                _logger.LogDebug("[Query {Index}/{Total}] Searching: '{Variant}'",
                    queryIndex, queryVariants.Count, variant);

                try
                {
                    var results = await _webSearchService.SearchAsync(variant, count: 10, offset: 0, cancellationToken);

                    if (results != null && results.Any())
                    {
                        aggregatedResults.AddRange(results);
                    }
                    else
                    {
                        _logger.LogDebug("  ✗ No results returned");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "  ⚠️ Query search failed");
                }
            }

            if (!aggregatedResults.Any())
            {
                _logger.LogWarning("❌ No search results returned across ANY query variants");
                return null;
            }

            _logger.LogDebug("Aggregated {Count} raw results across all variants, filtering...",
                aggregatedResults.Count);

            // Filter and prioritize results
            var relevantUrls = FilterAndPrioritizeResults(aggregatedResults, topic,
                technologyKeywords, category);

            if (!relevantUrls.Any())
            {
                _logger.LogWarning("❌ All results filtered out (no relevant URLs found)");
                return null;
            }

            // Deduplicate within this session's results + canonicalize
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ⭐ CRITICAL FIX: Canonicalize existing URLs before creating the HashSet
            var existingSet = new HashSet<string>(
                readingsList
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => CanonicalizeUrl(r.Trim())),
                StringComparer.OrdinalIgnoreCase);

            relevantUrls = relevantUrls
                .Select(CanonicalizeUrl)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Where(u => !existingSet.Contains(u)) // Check against canonicalized set
                .Where(u => seen.Add(u))
                .ToList();

            if (!relevantUrls.Any())
            {
                _logger.LogWarning("❌ All results were duplicates or already in syllabus");
                return null;
            }

            _logger.LogInformation("✅ Filtering result: {RelevantCount}/{TotalCount} URLs passed relevance & dedup",
                relevantUrls.Count, aggregatedResults.Count);

            // Validate top URLs
            foreach (var url in relevantUrls)
            {
                // ⭐ CRITICAL: Just-in-time check against concurrent threads
                if (isUrlUsedCheck != null && isUrlUsedCheck(url))
                {
                    _logger.LogDebug("⏭️ Skipping URL found in concurrent execution: {Url}", url);
                    continue;
                }

                _logger.LogDebug("Validating URL: {Url}", url);

                if (await _urlValidationService.IsUrlAccessibleAsync(url, cancellationToken))
                {
                    // Double check in case another thread snatched it while we were validating
                    if (isUrlUsedCheck != null && isUrlUsedCheck(url))
                    {
                        _logger.LogDebug("⏭️ Another thread took this URL before we could return it: {Url}", url);
                        continue;
                    }

                    _logger.LogInformation("✅ [TIER 2] Found valid URL: {Url}", url);
                    return url;
                }
                else
                {
                    _logger.LogDebug("❌ URL validation failed (404/timeout): {Url}", url);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Web search failed");
        }

        // ============================================================================
        // TIER 3: Fallback to official documentation
        // ============================================================================
        _logger.LogWarning("[TIER 3] Using official documentation as last resort...");

        var officialDocUrl = GetOfficialDocumentationUrl(topic, technologyKeywords, category);
        if (!string.IsNullOrWhiteSpace(officialDocUrl))
        {
            // Check uniqueness for official doc too
            if (isUrlUsedCheck != null && isUrlUsedCheck(officialDocUrl))
            {
                _logger.LogWarning("⏭️ Skipping official doc (already used): {Url}", officialDocUrl);
            }
            else
            {
                _logger.LogWarning("⚠️ [TIER 3] Attempting official doc: {Url}", officialDocUrl);

                if (await _urlValidationService.IsUrlAccessibleAsync(officialDocUrl, cancellationToken))
                {
                    _logger.LogInformation("✅ [TIER 3] Using official doc (fallback): {Url}", officialDocUrl);
                    return officialDocUrl;
                }
            }
        }

        _logger.LogError("❌ COMPLETE FAILURE: No valid URL found for '{Topic}'", topic);
        return null;
    }

    #region LLM Query Generation

    /// <summary>
    /// Generate search queries using LLM (only used if no pre-generated queries provided).
    /// </summary>
    private async Task<List<string>> GenerateSearchQueriesWithLLM(
        string topic,
        string? subjectContext,
        SubjectCategory category,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🤖 Generating queries via LLM for '{Topic}'", topic);

            var response = await _aiQueryService.GenerateQueryVariantsAsync(
                topic, subjectContext, category, cancellationToken);

            if (response != null && response.Any())
            {
                _logger.LogInformation("✅ LLM generated {Count} queries", response.Count);
                return response;
            }

            _logger.LogWarning("⚠️ LLM returned no queries");
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ LLM query generation failed");
            return new List<string>();
        }
    }

    #endregion

    #region Rule-Based Query Generation (Fallback)

    /// <summary>
    /// Build search query variants using rule-based logic (fallback only).
    /// </summary>
    private List<string> BuildQueryVariants(string topic, string? subjectContext, SubjectCategory category)
    {
        var queries = new List<string>();

        switch (category)
        {
            case SubjectCategory.Programming:
            case SubjectCategory.ComputerScience:
                if (!string.IsNullOrWhiteSpace(subjectContext))
                {
                    var contextTokens = subjectContext.Split(new[] { ',', ' ', '-' },
                        StringSplitOptions.RemoveEmptyEntries).Take(2);
                    queries.Add($"{string.Join(" ", contextTokens)} {topic} tutorial");
                    queries.Add($"{string.Join(" ", contextTokens)} {topic} guide");
                    queries.Add($"{topic} documentation");
                }
                else
                {
                    queries.Add($"{topic} tutorial guide");
                    queries.Add($"{topic} example code");
                    queries.Add($"{topic} documentation");
                }
                break;

            case SubjectCategory.VietnamesePolitics:
                queries.Add($"{topic} lý thuyết bài giảng");
                queries.Add($"{topic} tài liệu học tập");
                queries.Add($"{topic} giáo trình");
                break;

            case SubjectCategory.History:
                queries.Add($"{topic} tài liệu lịch sử");
                queries.Add($"{topic} giáo trình");
                queries.Add($"{topic} lịch sử analysis");
                break;

            case SubjectCategory.VietnameseLiterature:
                queries.Add($"{topic} bài tập trắc nghiệm");
                queries.Add($"{topic} tài liệu ôn tập");
                queries.Add($"{topic} văn học");
                break;

            case SubjectCategory.Science:
                queries.Add($"{topic} lý thuyết công thức");
                queries.Add($"{topic} bài giảng");
                queries.Add($"{topic} scientific explanation");
                break;

            case SubjectCategory.Business:
                queries.Add($"{topic} bài giảng kinh tế");
                queries.Add($"{topic} giáo trình");
                queries.Add($"{topic} management guide");
                break;

            default:
                bool isVietnamese = topic.Contains(" và ") || topic.Contains(" của ") || topic.Contains(" là ");
                queries.Add(isVietnamese
                    ? $"{topic} tài liệu học tập"
                    : $"{topic} guide tutorial");
                queries.Add(isVietnamese
                    ? $"{topic} bài giảng"
                    : $"{topic} educational resource");
                break;
        }

        return queries.Distinct().ToList();
    }

    #endregion

    #region Helper Methods

    private static string CanonicalizeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var builder = new UriBuilder(uri) { Fragment = string.Empty };
            var qs = System.Web.HttpUtility.ParseQueryString(builder.Query);

            // Remove tracking params
            var toRemove = qs.AllKeys?
                .Where(k => k != null && k.StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<string>();

            foreach (var k in toRemove) qs.Remove(k);

            builder.Query = qs.ToString() ?? string.Empty;
            return builder.Uri.ToString().TrimEnd('/');
        }
        catch
        {
            return url.Trim();
        }
    }

    private List<string> ExtractTechnologyKeywords(string? subjectContext)
    {
        if (string.IsNullOrWhiteSpace(subjectContext))
            return new List<string>();

        var contextLower = subjectContext.ToLowerInvariant();
        var keywords = new List<string>();

        // Mobile/Android
        if (contextLower.Contains("android")) keywords.Add("android");
        if (contextLower.Contains("mobile")) keywords.Add("mobile");
        if (contextLower.Contains("kotlin")) keywords.Add("kotlin");

        // Java (NOT JavaScript)
        if (contextLower.Contains("java") && !contextLower.Contains("javascript"))
        {
            keywords.Add("java");
            if (contextLower.Contains("servlet") || contextLower.Contains("jsp"))
                keywords.Add("java-web");
            if (contextLower.Contains("spring"))
                keywords.Add("spring");
        }

        // .NET
        if (contextLower.Contains("asp.net") || contextLower.Contains("aspnet")) keywords.Add("asp.net");
        if (contextLower.Contains("c#") || contextLower.Contains("csharp")) keywords.Add("c#");
        if (contextLower.Contains(".net") && !contextLower.Contains("dotnet.vn")) keywords.Add(".net");

        // Node.js
        if (contextLower.Contains("node")) keywords.Add("nodejs");
        if (contextLower.Contains("express")) keywords.Add("express");

        // Frontend
        if (contextLower.Contains("react")) keywords.Add("react");
        if (contextLower.Contains("vue")) keywords.Add("vue");
        if (contextLower.Contains("angular")) keywords.Add("angular");
        if (contextLower.Contains("javascript") && !contextLower.Contains("java ")) keywords.Add("javascript");
        if (contextLower.Contains("typescript")) keywords.Add("typescript");

        // Other
        if (contextLower.Contains("python")) keywords.Add("python");
        if (contextLower.Contains("flutter")) keywords.Add("flutter");
        if (contextLower.Contains("ios") || contextLower.Contains("swift")) keywords.Add("ios");

        return keywords.Distinct().ToList();
    }

    private string? ExtractUrlFromSearchResult(string searchResult)
    {
        var lines = searchResult.Split('\n');
        var urlLine = lines.FirstOrDefault(line =>
            line.StartsWith("Link: ", StringComparison.OrdinalIgnoreCase));

        return urlLine?.Substring("Link: ".Length).Trim();
    }

    private string? GetOfficialDocumentationUrl(string topic, List<string> technologyKeywords, SubjectCategory category)
    {
        if (category != SubjectCategory.Programming && category != SubjectCategory.ComputerScience)
            return null;

        var topicLower = topic.ToLowerInvariant();

        // Android
        if (technologyKeywords.Contains("android"))
        {
            if (topicLower.Contains("activity"))
                return "https://developer.android.com/guide/components/activities";
            if (topicLower.Contains("recyclerview"))
                return "https://developer.android.com/develop/ui/views/layout/recyclerview";
            if (topicLower.Contains("fragment"))
                return "https://developer.android.com/guide/fragments";
            return "https://developer.android.com/guide";
        }

        // ASP.NET
        if (technologyKeywords.Contains("asp.net") || technologyKeywords.Contains("c#"))
        {
            if (topicLower.Contains("mvc"))
                return "https://learn.microsoft.com/en-us/aspnet/core/mvc/overview";
            return "https://learn.microsoft.com/en-us/aspnet/core/";
        }

        // React
        if (technologyKeywords.Contains("react"))
        {
            if (topicLower.Contains("hook"))
                return "https://react.dev/reference/react/hooks";
            return "https://react.dev/learn";
        }

        return null;
    }

    #endregion

    #region Filtering & Scoring

    private List<string> FilterAndPrioritizeResults(
        IEnumerable<string> searchResults,
        string topic,
        List<string> technologyKeywords,
        SubjectCategory category)
    {
        var scoredUrls = new List<(string Url, int Score)>();

        foreach (var result in searchResults)
        {
            var url = ExtractUrlFromSearchResult(result);
            if (string.IsNullOrWhiteSpace(url)) continue;

            if (IsUntrustedSource(url, category))
            {
                _logger.LogDebug("  → 🚫 BLOCKED (untrusted): {Url}", url);
                continue;
            }

            if ((category == SubjectCategory.Programming || category == SubjectCategory.ComputerScience) &&
                IsWrongFramework(url, technologyKeywords, category, topic))
            {
                _logger.LogDebug("  → 🚫 BLOCKED (wrong framework): {Url}", url);
                continue;
            }

            int score = CalculateRelevanceScore(url, result, topic, technologyKeywords, category);

            if (score > 0)
            {
                scoredUrls.Add((url, score));
                _logger.LogDebug("  → ✓ {Score} pts: {Url}", score, url);
            }
        }

        return scoredUrls
            .OrderByDescending(x => x.Score)
            .Select(x => x.Url)
            .ToList();
    }

    private bool IsUntrustedSource(string url, SubjectCategory category)
    {
        var urlLower = url.ToLowerInvariant();

        // Universal blocks
        var universalBlocks = new[] { "scribd.com", "academia.edu", "researchgate.net", "coursehero.com" };
        if (universalBlocks.Any(b => urlLower.Contains(b)))
            return true;

        // Programming/CS: Block forums
        if (category == SubjectCategory.Programming || category == SubjectCategory.ComputerScience)
        {
            var programmingBlocks = new[] { "reddit.com", "quora.com", "stackoverflow.com/questions" };
            if (programmingBlocks.Any(b => urlLower.Contains(b)))
                return true;
        }

        return false;
    }

    private bool IsWrongFramework(string url, List<string> technologyKeywords, SubjectCategory category, string topic)
    {
        if (category != SubjectCategory.Programming && category != SubjectCategory.ComputerScience)
            return false;

        var urlLower = url.ToLowerInvariant();
        var uri = new Uri(url);
        var path = uri.AbsolutePath.ToLowerInvariant();

        // Java subjects: Block .NET/Python
        if (technologyKeywords.Contains("java"))
        {
            var wrongPaths = new[] { "/asp/", "/aspnet/", "/csharp/", "/python/", "/php/" };
            if (wrongPaths.Any(p => path.Contains(p)))
                return true;
        }

        // .NET subjects: Block Java/Python
        if (technologyKeywords.Contains("asp.net") || technologyKeywords.Contains("c#"))
        {
            var wrongPaths = new[] { "/java/", "/jsp/", "/servlet/", "/python/" };
            if (wrongPaths.Any(p => path.Contains(p)))
                return true;
        }

        // Android: Block Flutter/React Native
        if (technologyKeywords.Contains("android"))
        {
            if (urlLower.Contains("flutter") || urlLower.Contains("react-native"))
                return true;
        }

        return false;
    }

    private int CalculateRelevanceScore(
        string url,
        string fullResult,
        string topic,
        List<string> technologyKeywords,
        SubjectCategory category)
    {
        var urlLower = url.ToLowerInvariant();
        int score = 0;

        // Tutorial sites (high trust)
        var tutorialSites = new[] { "tutorialspoint.com", "w3schools.com", "geeksforgeeks.org", "javatpoint.com", "programiz.com" };
        if (tutorialSites.Any(s => urlLower.Contains(s)))
            score += 1000;

        // Official docs (highest priority)
        var officialDocs = new[] { "developer.android.com", "learn.microsoft.com", "docs.oracle.com", "react.dev", "python.org" };
        if (officialDocs.Any(s => urlLower.Contains(s)))
            score += 1200;

        // Technology keyword matches
        foreach (var keyword in technologyKeywords)
        {
            if (urlLower.Contains(keyword))
                score += 500;
        }

        // Category-specific boosts
        if (category == SubjectCategory.VietnamesePolitics && urlLower.Contains("dangcongsan.vn"))
            score += 1500;

        if (category == SubjectCategory.VietnameseLiterature && urlLower.Contains("vietjack.com"))
            score += 1200;

        // Topic keyword matching (weight by word length)
        var topicTokens = topic.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in topicTokens.Where(t => t.Length > 3))
        {
            if (urlLower.Contains(token))
                score += 200;
        }

        return score;
    }

    #endregion
}
