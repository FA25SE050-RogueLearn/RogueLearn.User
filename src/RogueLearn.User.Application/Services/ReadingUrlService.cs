// RogueLearn.User/src/RogueLearn.User.Application/Services/ReadingUrlService.cs
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Services;

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

    public async Task<string?> GetValidUrlForTopicAsync(
        string topic,
        IEnumerable<string> readings,
        string? subjectContext = null,
        SubjectCategory category = SubjectCategory.General,
        List<string>? overrideQueries = null,
        Func<string, bool>? isUrlUsedCheck = null,
        CancellationToken cancellationToken = default)
    {
        var readingsList = readings?.ToList() ?? new List<string>();

        _logger.LogInformation(
            "🔍 [Session Search] Topic: '{Topic}' | Context: '{Context}' | Category: {Category}",
            topic, subjectContext ?? "none", category);

        // 1. DELEGATE to ContextKeywordExtractor
        // This ensures we get the "C" vs "C#" distinction defined in the helper class
        var technologyKeywords = ContextKeywordExtractor.ExtractTechnologyKeywords(subjectContext);
        _logger.LogDebug("Detected technologies: {Technologies}", string.Join(", ", technologyKeywords));

        // ============================================================================
        // TIER 1: Check syllabus readings first
        // ============================================================================
        _logger.LogDebug("[TIER 1] Checking {ReadingCount} existing syllabus readings...", readingsList.Count);

        foreach (var reading in readingsList)
        {
            if (string.IsNullOrWhiteSpace(reading)) continue;
            if (isUrlUsedCheck != null && isUrlUsedCheck(reading)) continue;

            if (Uri.TryCreate(reading, UriKind.Absolute, out var uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                _logger.LogDebug("[TIER 1] Validating syllabus URL: '{Url}'", reading);

                if (await _urlValidationService.IsUrlAccessibleAsync(reading, cancellationToken))
                {
                    // DELEGATE to RelevanceScorer (via the same logic used in filtering)
                    // We create a dummy "fullResult" since we only have the URL
                    var score = RelevanceScorer.CalculateRelevanceScore(
                        reading,
                        $"Link: {reading}",
                        topic,
                        technologyKeywords,
                        category);

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
        // TIER 2: Web search with rate limiting
        // ============================================================================
        _logger.LogInformation("[TIER 2] Searching with AI-generated queries to find diverse URLs...");

        try
        {
            List<string> queryVariants;

            if (overrideQueries != null && overrideQueries.Any())
            {
                queryVariants = overrideQueries;
                _logger.LogInformation(
                    "📋 Using {QueryCount} PRE-GENERATED AI queries (session-specific, diverse)",
                    queryVariants.Count);
            }
            else
            {
                _logger.LogWarning("⚠️ No pre-generated queries provided. Generating inline...");
                queryVariants = await GenerateSearchQueriesWithLLM(topic, subjectContext, category, cancellationToken);

                if (!queryVariants.Any())
                {
                    _logger.LogWarning("LLM query generation failed, falling back to rule-based queries");
                    // DELEGATE to SearchQueryBuilder
                    queryVariants = SearchQueryBuilder.BuildQueryVariants(topic, subjectContext, category);
                }
            }

            // ⭐ RATE LIMITING: Execute searches with delays
            var aggregatedResults = new List<string>();
            int queryIndex = 0;

            foreach (var variant in queryVariants)
            {
                queryIndex++;
                _logger.LogDebug("[Query {QueryIndex}/{QueryTotal}] Searching: '{SearchVariant}'",
                    queryIndex, queryVariants.Count, variant);

                try
                {
                    // Add delay between queries to avoid 429 rate limit
                    if (queryIndex > 1)
                    {
                        await Task.Delay(500, cancellationToken);
                    }

                    var results = await _webSearchService.SearchAsync(variant, count: 10, offset: 0, cancellationToken);

                    if (results != null && results.Any())
                    {
                        aggregatedResults.AddRange(results);
                        _logger.LogDebug("  ✓ Got {ResultCount} results", results.Count());
                    }
                }
                catch (HttpRequestException ex) when ((int?)ex.StatusCode == 429)
                {
                    _logger.LogWarning("⚠️ 429 Too Many Requests - backing off...");
                    await Task.Delay(2000, cancellationToken);
                    // Simple retry logic could go here
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "  ⚠️ Query search failed: {ExceptionMessage}", ex.Message);
                }
            }

            if (!aggregatedResults.Any())
            {
                _logger.LogWarning("❌ No search results returned across ANY query variants");
                return null;
            }

            _logger.LogDebug("Aggregated {AggregateResultCount} raw results across all variants, filtering...",
                aggregatedResults.Count);

            // 2. DELEGATE to SearchResultFilter
            // This applies the strict language filtering you requested (e.g., blocking Python for C subjects)
            var relevantUrls = SearchResultFilter.FilterAndPrioritizeResults(
                aggregatedResults,
                topic,
                technologyKeywords,
                category,
                _logger); // Pass logger to debug blocked URLs

            if (!relevantUrls.Any())
            {
                _logger.LogWarning("❌ All results filtered out (no relevant URLs found)");
                return null;
            }

            // Deduplicate within this session's results
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingSet = new HashSet<string>(readingsList, StringComparer.OrdinalIgnoreCase);

            relevantUrls = relevantUrls
                .Where(u => !existingSet.Contains(u))
                .Where(u => seen.Add(u))
                .ToList();

            if (!relevantUrls.Any())
            {
                _logger.LogWarning("❌ All results were duplicates or already in syllabus");
                return null;
            }

            _logger.LogInformation("✅ Filtering result: {RelevantUrlCount}/{TotalResultCount} URLs passed relevance & dedup",
                relevantUrls.Count, aggregatedResults.Count);

            // Validate top URLs
            foreach (var url in relevantUrls)
            {
                if (isUrlUsedCheck != null && isUrlUsedCheck(url))
                {
                    _logger.LogDebug("⏭️ Skipping URL found in concurrent execution: {Url}", url);
                    continue;
                }

                _logger.LogDebug("Validating URL: {Url}", url);

                if (await _urlValidationService.IsUrlAccessibleAsync(url, cancellationToken))
                {
                    if (isUrlUsedCheck != null && isUrlUsedCheck(url))
                    {
                        _logger.LogDebug("⏭️ Another thread took this URL before we could return it: {Url}", url);
                        continue;
                    }

                    _logger.LogInformation("✅ [TIER 2] Found valid URL: {Url}", url);
                    return url;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Web search failed: {ExceptionMessage}", ex.Message);
        }

        // ============================================================================
        // TIER 3: Fallback to official documentation
        // ============================================================================
        _logger.LogWarning("[TIER 3] Using official documentation as last resort...");

        // 3. DELEGATE to OfficialDocsProvider
        var officialDocUrl = OfficialDocsProvider.GetOfficialDocumentationUrl(topic, technologyKeywords, category);

        if (!string.IsNullOrWhiteSpace(officialDocUrl))
        {
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
                topic, subjectContext!, category, cancellationToken);
            return response ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ LLM query generation failed: {ExceptionMessage}", ex.Message);
            return new List<string>();
        }
    }
}