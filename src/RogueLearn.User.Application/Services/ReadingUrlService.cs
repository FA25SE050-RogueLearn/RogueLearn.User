// RogueLearn.User/src/RogueLearn.User.Application/Services/ReadingUrlService.cs
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using System.Text.RegularExpressions;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Subject category for context-aware URL sourcing.
/// </summary>
public enum SubjectCategory
{
    Programming,
    ComputerScience,  // NEW: Theory-based CS (architecture, systems, etc.)
    VietnamesePolitics,
    History,
    VietnameseLiterature,
    Business,
    Science,
    General
}

/// <summary>
/// Sources valid, accessible URLs with CONTEXT-AWARE and CATEGORY-AWARE relevance checking.
/// Supports both technical and non-technical subjects.
/// Priority: Syllabus URLs → Category-Specific Sites → Web Search → Official Docs
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

    public async Task<string?> GetValidUrlForTopicAsync(
        string topic,
        IEnumerable<string> readings,
        string? subjectContext = null,
        SubjectCategory category = SubjectCategory.General,
        CancellationToken cancellationToken = default)
    {
        var readingsList = readings?.ToList() ?? new List<string>();

        _logger.LogInformation("🔍 Starting URL search | Topic: '{Topic}' | Context: '{Context}' | Category: {Category}",
            topic, subjectContext ?? "none", category);

        var normalizedTopic = TopicNormalizer.Normalize(topic);
        if (TopicNormalizer.IsMetaSession(normalizedTopic))
        {
            _logger.LogWarning("Skipping URL search for meta session: '{Topic}'", topic);
            return null;
        }

        // Extract technology keywords from context
        var technologyKeywords = ContextKeywordExtractor.ExtractTechnologyKeywords(subjectContext);
        _logger.LogDebug("Detected technologies: {Technologies}", string.Join(", ", technologyKeywords));

        // TIER 1: Check syllabus readings
        foreach (var reading in readingsList)
        {
            if (string.IsNullOrWhiteSpace(reading)) continue;

            if (Uri.TryCreate(reading, UriKind.Absolute, out var uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                _logger.LogDebug("[TIER 1] Validating syllabus URL: '{Url}'", reading);

                if (await _urlValidationService.IsUrlAccessibleAsync(reading, cancellationToken))
                {
                    _logger.LogInformation("✅ [TIER 1] Syllabus URL: {Url}", reading);
                    return reading;
                }
            }
        }

        // TIER 2: Web search with CONTEXT-AWARE and CATEGORY-AWARE filtering
        _logger.LogInformation("🌐 [TIER 2] Searching with category-aware filtering");

        try
        {
            // Build category-specific query
            var searchQuery = SearchQueryBuilder.BuildContextAwareQuery(normalizedTopic, subjectContext, category);
            _logger.LogDebug("Search query: '{Query}'", searchQuery);

            var searchResults = await _webSearchService.SearchAsync(
                searchQuery, count: 15, offset: 0, cancellationToken);

            if (searchResults == null || !searchResults.Any())
            {
                _logger.LogWarning("No search results returned");
                if (category == SubjectCategory.Science)
                {
                    var altQuery = SearchQueryBuilder.BuildContextAwareQuery(normalizedTopic, null, SubjectCategory.General);
                    _logger.LogDebug("Alternate search query: '{Query}'", altQuery);
                    searchResults = await _webSearchService.SearchAsync(altQuery, count: 15, offset: 0, cancellationToken);
                    if (searchResults == null || !searchResults.Any()) return null;
                }
            }

            _logger.LogDebug("Found {Count} raw results, filtering for relevance...", searchResults.Count());

            // Filter and prioritize results
            var relevantUrls = SearchResultFilter.FilterAndPrioritizeResults(searchResults, normalizedTopic, technologyKeywords, category, _logger);

            if (!relevantUrls.Any())
            {
                _logger.LogWarning("❌ All results filtered out as irrelevant");
                return null;
            }

            _logger.LogInformation("✅ {RelevantCount}/{TotalCount} results passed relevance check",
                relevantUrls.Count, searchResults.Count());

            // Try each relevant URL
            foreach (var url in relevantUrls)
            {
                _logger.LogDebug("Validating: {Url}", url);

                if (await _urlValidationService.IsUrlAccessibleAsync(url, cancellationToken))
                {
                    _logger.LogInformation("✅ [TIER 2] Found valid URL: {Url}", url);
                    return url;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web search failed for topic '{Topic}'", topic);
        }

        // TIER 3: Fallback to official docs (last resort)
        var officialDocUrl = OfficialDocsProvider.GetOfficialDocumentationUrl(topic, technologyKeywords, category);
        if (!string.IsNullOrWhiteSpace(officialDocUrl))
        {
            _logger.LogWarning("⚠️ [TIER 3] Using official doc (last resort): {Url}", officialDocUrl);

            if (await _urlValidationService.IsUrlAccessibleAsync(officialDocUrl, cancellationToken))
            {
                return officialDocUrl;
            }
        }

        _logger.LogError("❌ FAILED: No valid URL found for '{Topic}'", topic);
        return null;
    }

    /// <summary>
    /// Extract technology keywords from subject context.
    /// </summary>
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

        // Java (NOT JavaScript) - Check carefully
        if (contextLower.Contains("java") && !contextLower.Contains("javascript"))
        {
            keywords.Add("java");

            // Specific Java web frameworks
            if (contextLower.Contains("servlet") || contextLower.Contains("jsp"))
                keywords.Add("java-web");
            if (contextLower.Contains("spring"))
                keywords.Add("spring");
        }

        // Web - Backend (.NET)
        if (contextLower.Contains("asp.net") || contextLower.Contains("aspnet")) keywords.Add("asp.net");
        if (contextLower.Contains("c#") || contextLower.Contains("csharp")) keywords.Add("c#");
        if (contextLower.Contains(".net") && !contextLower.Contains("dotnet.vn")) keywords.Add(".net");

        // Web - Backend (Node.js)
        if (contextLower.Contains("node")) keywords.Add("nodejs");
        if (contextLower.Contains("express")) keywords.Add("express");

        // Web - Frontend
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

    /// <summary>
    /// Build search query with subject context and category awareness.
    /// NOW INCLUDES LANGUAGE DETECTION for General category.
    /// </summary>
    private string BuildContextAwareQuery(string topic, string? subjectContext, SubjectCategory category)
    {
        switch (category)
        {
            case SubjectCategory.Programming:
                // Programming: "Android tutorial guide"
                if (!string.IsNullOrWhiteSpace(subjectContext))
                {
                    var contextTokens = subjectContext.Split(new[] { ',', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries).Take(3);
                    return $"{string.Join(" ", contextTokens)} {topic} tutorial";
                }
                return $"{topic} tutorial guide";

            case SubjectCategory.ComputerScience:
                // Computer Science: "Computer architecture guide tutorial"
                return $"{topic} guide tutorial explanation";

            case SubjectCategory.VietnamesePolitics:
                // Vietnamese Politics: "Tư tưởng Hồ Chí Minh lý thuyết bài giảng"
                return $"{topic} lý thuyết bài giảng";

            case SubjectCategory.History:
                // History: "Lịch sử Việt Nam tài liệu"
                return $"{topic} tài liệu lịch sử";

            case SubjectCategory.VietnameseLiterature:
                // Literature: "Ngữ văn bài tập trắc nghiệm"
                return $"{topic} bài tập trắc nghiệm";

            case SubjectCategory.Science:
                // Science: "Toán học lý thuyết công thức"
                return $"{topic} lý thuyết công thức";

            case SubjectCategory.Business:
                // Business: "Kinh tế học bài giảng"
                return $"{topic} bài giảng kinh tế";

            default:
                // FIXED: Detect language to avoid mixing English/Vietnamese
                bool isVietnamese = topic.Contains(" và ") ||
                                   topic.Contains(" của ") ||
                                   topic.Contains(" là ") ||
                                   topic.Contains(" được ") ||
                                   topic.Contains(" trong ");

                if (isVietnamese)
                {
                    return $"{topic} tài liệu học tập";
                }
                else
                {
                    // For English topics in General category
                    return $"{topic} guide tutorial explanation";
                }
        }
    }

    /// <summary>
    /// Filter and prioritize search results by category and relevance.
    /// </summary>
    private List<string> FilterAndPrioritizeResults(
        IEnumerable<string> searchResults,
        string topic,
        List<string> technologyKeywords,
        SubjectCategory category)
    {
        var scoredUrls = new List<(string Url, int Score)>();

        foreach (var result in searchResults)
        {
            var url = SearchResultParser.ExtractUrlFromSearchResult(result);
            if (string.IsNullOrWhiteSpace(url)) continue;

            // ⭐ FILTER 1: Block category-specific untrusted sources
            if (IsUntrustedSource(url, category))
            {
                _logger.LogDebug("  → 🚫 BLOCKED (untrusted source): {Url}", url);
                continue;
            }

            // ⭐ FILTER 2: Block wrong frameworks/content
            if ((category == SubjectCategory.Programming || category == SubjectCategory.ComputerScience) &&
                IsWrongFramework(url, technologyKeywords, category, topic))
            {
                _logger.LogDebug("  → 🚫 BLOCKED (wrong framework/content): {Url}", url);
                continue;
            }

            // Calculate category-aware relevance score
            int score = CalculateRelevanceScore(url, result, topic, technologyKeywords, category);

            if (score > 0)
            {
                scoredUrls.Add((url, score));
                _logger.LogDebug("  → {Score} pts: {Url}", score, url);
            }
            else
            {
                _logger.LogDebug("  → ❌ BLOCKED (low relevance): {Url}", url);
            }
        }

        // Return URLs sorted by score (highest first)
        return scoredUrls
            .OrderByDescending(x => x.Score)
            .Select(x => x.Url)
            .ToList();
    }

    /// <summary>
    /// Check if URL is from an untrusted source (CATEGORY-AWARE).
    /// </summary>
    private bool IsUntrustedSource(string url, SubjectCategory category)
    {
        var urlLower = url.ToLowerInvariant();

        // Universal blocks (paywalls) for ALL subjects
        foreach (var blocked in Sources.UniversalBlockedSources)
        {
            if (urlLower.Contains(blocked))
            {
                _logger.LogDebug("🚫 Universal block: {Pattern}", blocked);
                return true;
            }
        }

        // Category-specific blocking
        switch (category)
        {
            case SubjectCategory.Programming:
            case SubjectCategory.ComputerScience:
                // For programming/CS: Block forums, discussions, etc.
                foreach (var untrusted in Sources.UntrustedSourcesForProgramming)
                {
                    if (urlLower.Contains(untrusted))
                    {
                        _logger.LogDebug("🚫 Programming/CS-specific block: {Pattern}", untrusted);
                        return true;
                    }
                }
                break;

            case SubjectCategory.VietnamesePolitics:
            case SubjectCategory.History:
            case SubjectCategory.VietnameseLiterature:
                // For theory subjects: ALLOW news, Wikipedia, forums (discussions are valuable)
                // Only block paywalls (already handled by universal blocks)
                return false;

            case SubjectCategory.Science:
            case SubjectCategory.Business:
                // For science/business: Allow academic sources, news
                // Only block paywalls (already handled by universal blocks)
                return false;

            default:
                return false;
        }

        return false;
    }

    /// <summary>
    /// Check if URL is about the wrong technology/framework.
    /// NOW SUPPORTS: Programming category AND ComputerScience category.
    /// </summary>
    private bool IsWrongFramework(string url, List<string> technologyKeywords, SubjectCategory category, string topic)
    {
        var urlLower = url.ToLowerInvariant();

        // PROGRAMMING CATEGORY: Framework-specific blocking
        if (category == SubjectCategory.Programming)
        {
            // JAVA-SPECIFIC BLOCKING: Block .NET/C# content for Java subjects
            if (technologyKeywords.Contains("java") && !technologyKeywords.Contains("javascript"))
            {
                // Only block if the URL PATH is specifically about .NET (not just mentions it)
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                // Block dedicated .NET tutorial pages
                var dotNetPaths = new[] {
                    "/asp/", "/aspnet/", "/dotnet/", "/csharp/", "/cs/",
                    "/asp.net/", "/c-sharp/", "/vb.net/"
                };

                if (dotNetPaths.Any(pattern => path.Contains(pattern)))
                {
                    _logger.LogDebug("❌ Wrong language path: .NET/C# (expected Java) - Path: {Path}", path);
                    return true;
                }

                // Block W3Schools .NET pages specifically
                if (urlLower.Contains("w3schools.com") &&
                    (path.Contains("/asp/") || path.Contains("/cs/")))
                {
                    _logger.LogDebug("❌ W3Schools .NET page (expected Java)");
                    return true;
                }

                // Block PHP tutorial pages for Java subjects
                var phpPaths = new[] { "/php/", "/php5/", "/php7/" };
                if (phpPaths.Any(pattern => path.Contains(pattern)))
                {
                    _logger.LogDebug("❌ Wrong language: PHP (expected Java)");
                    return true;
                }

                // Block Python tutorial pages for Java subjects (unless DS/Algo comparison)
                var topicLower = topic.ToLowerInvariant();
                var isPythonPath = path.Contains("/python/") || path.Contains("/py/");
                var isComparisonTopic = topicLower.Contains("comparison") || topicLower.Contains("vs") ||
                                       topicLower.Contains("difference");

                if (isPythonPath && !isComparisonTopic)
                {
                    _logger.LogDebug("❌ Wrong language: Python tutorial (expected Java)");
                    return true;
                }
            }

            // ASP.NET-SPECIFIC BLOCKING: Block Java/Spring content for .NET subjects
            if (technologyKeywords.Contains("asp.net") || technologyKeywords.Contains("c#") || technologyKeywords.Contains(".net"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                // Block dedicated Java tutorial pages
                var javaPaths = new[] {
                    "/java/", "/jsp/", "/servlet/", "/spring/",
                    "/java-ee/", "/jakarta/"
                };

                if (javaPaths.Any(pattern => path.Contains(pattern)))
                {
                    _logger.LogDebug("❌ Wrong language path: Java (expected ASP.NET/C#) - Path: {Path}", path);
                    return true;
                }
            }

            // ANDROID-SPECIFIC BLOCKING: Block Flutter and web-specific content
            if (technologyKeywords.Contains("android") || technologyKeywords.Contains("kotlin") || technologyKeywords.Contains("mobile"))
            {
                if (urlLower.Contains("flutter") || urlLower.Contains("/flutter/"))
                {
                    _logger.LogDebug("❌ Wrong framework: Flutter (expected Android)");
                    return true;
                }

                if (urlLower.Contains("react-native") || urlLower.Contains("reactnative"))
                {
                    _logger.LogDebug("❌ Wrong framework: React Native (expected Android)");
                    return true;
                }

                if (urlLower.Contains("xamarin"))
                {
                    _logger.LogDebug("❌ Wrong framework: Xamarin (expected Android)");
                    return true;
                }

                if (urlLower.Contains("localstorage") || urlLower.Contains("local-storage") || urlLower.Contains("sessionstorage"))
                {
                    _logger.LogDebug("❌ Web content (expected Android native)");
                    return true;
                }

                if (urlLower.Contains("chrome-extension") || urlLower.Contains("chrome/extension"))
                {
                    _logger.LogDebug("❌ Chrome extension content (expected Android)");
                    return true;
                }
            }

            // REACT-SPECIFIC BLOCKING: Block Angular/Vue
            if (technologyKeywords.Contains("react"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                if (path.Contains("/angular/") || path.Contains("/vue/"))
                {
                    _logger.LogDebug("❌ Wrong framework (expected React)");
                    return true;
                }
            }

            // VUE-SPECIFIC BLOCKING: Block React/Angular
            if (technologyKeywords.Contains("vue"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                if (path.Contains("/react/") || path.Contains("/angular/"))
                {
                    _logger.LogDebug("❌ Wrong framework (expected Vue)");
                    return true;
                }
            }

            // ANGULAR-SPECIFIC BLOCKING: Block React/Vue
            if (technologyKeywords.Contains("angular"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                if (path.Contains("/react/") || path.Contains("/vue/"))
                {
                    _logger.LogDebug("❌ Wrong framework (expected Angular)");
                    return true;
                }
            }

            // PYTHON-SPECIFIC BLOCKING: Block Java/.NET content
            if (technologyKeywords.Contains("python"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                var otherLanguagePaths = new[] { "/java/", "/csharp/", "/asp/", "/dotnet/" };
                if (otherLanguagePaths.Any(pattern => path.Contains(pattern)))
                {
                    _logger.LogDebug("❌ Wrong language (expected Python)");
                    return true;
                }
            }
        }

        // COMPUTER SCIENCE CATEGORY: Block irrelevant tutorial site sections
        if (category == SubjectCategory.ComputerScience)
        {
            var topicLower = topic.ToLowerInvariant();

            // Block W3Schools web development sections for CS theory topics
            if (urlLower.Contains("w3schools.com"))
            {
                // Assembly Language, Architecture, OS - NO web dev
                if (topicLower.Contains("assembly") || topicLower.Contains("architecture") ||
                    topicLower.Contains("operating system") || topicLower.Contains("computer organization"))
                {
                    var webDevSections = new[] { "/html/", "/css/", "/js/", "/javascript/", "/react/", "/vue/", "/angular/", "/bootstrap/" };
                    if (webDevSections.Any(section => urlLower.Contains(section)))
                    {
                        _logger.LogDebug("❌ Wrong W3Schools section: {Url} (expected CS theory, got web dev)", url);
                        return true;
                    }
                }

                // Data Structures - only allow Python/Java/C++, not web languages
                if (topicLower.Contains("data structure") || topicLower.Contains("algorithm"))
                {
                    var webDevSections = new[] { "/html/", "/css/", "/js/", "/javascript/", "/react/", "/vue/", "/sql/" };
                    if (webDevSections.Any(section => urlLower.Contains(section)))
                    {
                        _logger.LogDebug("❌ Wrong W3Schools section: {Url} (expected DS/Algo, got web dev)", url);
                        return true;
                    }
                }
            }

            // Block TutorialsPoint web development for CS theory
            if (urlLower.Contains("tutorialspoint.com"))
            {
                if (topicLower.Contains("assembly") || topicLower.Contains("architecture") ||
                    topicLower.Contains("operating system") || topicLower.Contains("computer organization"))
                {
                    var webDevSections = new[] { "/html/", "/css/", "/javascript/", "/reactjs/", "/vuejs/", "/angular/" };
                    if (webDevSections.Any(section => urlLower.Contains(section)))
                    {
                        _logger.LogDebug("❌ Wrong TutorialsPoint section: {Url} (expected CS theory, got web dev)", url);
                        return true;
                    }
                }
            }

            // Block GeeksforGeeks web development for pure CS theory
            if (urlLower.Contains("geeksforgeeks.org"))
            {
                if (topicLower.Contains("assembly") || topicLower.Contains("architecture") ||
                    topicLower.Contains("operating system"))
                {
                    var webDevKeywords = new[] { "-html-", "-css-", "-javascript-", "-react-", "-vue-" };
                    if (webDevKeywords.Any(keyword => urlLower.Contains(keyword)))
                    {
                        _logger.LogDebug("❌ Wrong GeeksforGeeks article: {Url} (expected CS theory, got web dev)", url);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Calculate relevance score (CATEGORY-AWARE).
    /// Higher score = more relevant. Score ≤ 0 = BLOCKED.
    /// NOW INCLUDES ComputerScience category scoring.
    /// </summary>
    private int CalculateRelevanceScore(
        string url,
        string fullResult,
        string topic,
        List<string> technologyKeywords,
        SubjectCategory category)
    {
        var urlLower = url.ToLowerInvariant();
        var resultLower = fullResult.ToLowerInvariant();
        int score = 0;

        // Category-specific scoring
        switch (category)
        {
            case SubjectCategory.Programming:
                // Tutorial sites = highest priority
                if (Sources.TutorialSites.Any(site => urlLower.Contains(site)))
                    score += 1000;

                // Community blogs
                if (Sources.CommunityBlogs.Any(site => urlLower.Contains(site)))
                    score += 900;

                // Technology keyword matches in URL
                foreach (var keyword in technologyKeywords)
                {
                    if (urlLower.Contains(keyword))
                        score += 500;
                    if (resultLower.Contains(keyword))
                        score += 100;
                }

                // JAVA-SPECIFIC SCORING: Boost Java-relevant URLs
                if (technologyKeywords.Contains("java") || technologyKeywords.Contains("java-web"))
                {
                    // Java-specific tutorial sites get extra boost
                    if (urlLower.Contains("/java/") || urlLower.Contains("/servlet/") ||
                        urlLower.Contains("/jsp/") || urlLower.Contains("java-"))
                        score += 600;

                    // Oracle official docs = very high priority for Java
                    if (urlLower.Contains("docs.oracle.com") || urlLower.Contains("oracle.com/javase"))
                        score += 800;

                    // Baeldung = excellent Java tutorials
                    if (urlLower.Contains("baeldung.com"))
                        score += 700;

                    // Java-specific terms in topic matched in URL
                    var topicLower = topic.ToLowerInvariant();
                    if (topicLower.Contains("servlet") && urlLower.Contains("servlet"))
                        score += 500;
                    if (topicLower.Contains("jsp") && urlLower.Contains("jsp"))
                        score += 500;
                    if (topicLower.Contains("jdbc") && urlLower.Contains("jdbc"))
                        score += 500;
                    if (topicLower.Contains("tomcat") && urlLower.Contains("tomcat"))
                        score += 400;
                }

                // Block wrong technology tutorials (W3Schools wrong sections)
                var wrongTechPatterns = new[]
                {
                    "w3schools.com/python", "w3schools.com/nodejs",
                    "w3schools.com/php", "w3schools.com/sql",
                    "programiz.com/cpp", "programiz.com/c-programming"
                };

                if (wrongTechPatterns.Any(pattern => urlLower.Contains(pattern)))
                {
                    // Only block if subject is NOT about that technology
                    var subjectTechs = string.Join(" ", technologyKeywords).ToLowerInvariant();
                    if (!subjectTechs.Contains("python") && urlLower.Contains("/python"))
                        return -500;
                    if (!subjectTechs.Contains("php") && urlLower.Contains("/php"))
                        return -500;
                    if (!subjectTechs.Contains("nodejs") && urlLower.Contains("/nodejs"))
                        return -500;
                }
                break;

            case SubjectCategory.ComputerScience:
                // Tutorial sites (GeeksforGeeks excellent for CS theory)
                if (Sources.TutorialSites.Any(site => urlLower.Contains(site)))
                    score += 1000;

                // Academic sources (Wikipedia, university sites)
                if (Sources.AcademicSources.Any(site => urlLower.Contains(site)))
                    score += 900;

                // Tech blogs (good explanations)
                if (Sources.CommunityBlogs.Any(site => urlLower.Contains(site)))
                    score += 800;

                // Specific CS theory terms in URL
                var csTerms = new[] {
                    "architecture", "organization", "performance",
                    "cache", "cpu", "memory", "pipeline", "instruction",
                    "computer-system", "operating-system", "assembly",
                    "assembly-language", "x86", "arm", "mips", "register"
                };
                foreach (var term in csTerms)
                {
                    if (urlLower.Contains(term))
                        score += 300;
                }

                // Use a different variable name to avoid CS0136
                var csTopicLower = topic.ToLowerInvariant();
                if (csTopicLower.Contains("assembly") && urlLower.Contains("assembly"))
                    score += 500;
                if (csTopicLower.Contains("architecture") && urlLower.Contains("architecture"))
                    score += 500;
                if (csTopicLower.Contains("operating") && urlLower.Contains("operating"))
                    score += 500;

                break;

            case SubjectCategory.VietnamesePolitics:
                // Official government/party sources = HIGHEST priority
                if (urlLower.Contains("dangcongsan.vn") || urlLower.Contains("chinhphu.vn"))
                    score += 1500;

                // Vietnamese educational sites
                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 1200;

                // News articles (theory/current events)
                if (urlLower.Contains("vnexpress.net") || urlLower.Contains("nhandan.vn") || urlLower.Contains("thanhnien.vn"))
                    score += 800;

                // Wikipedia
                if (urlLower.Contains("vi.wikipedia.org"))
                    score += 700;
                break;

            case SubjectCategory.History:
                // Wikipedia = excellent for history
                if (urlLower.Contains("wikipedia.org"))
                    score += 1000;

                // Vietnamese educational sites
                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 900;

                // Academic sources
                if (Sources.AcademicSources.Any(site => urlLower.Contains(site)))
                    score += 800;
                break;

            case SubjectCategory.VietnameseLiterature:
                // VietJack for quizzes/exercises = HIGHEST
                if (urlLower.Contains("vietjack.com"))
                    score += 1200;

                // Vietnamese Wikipedia
                if (urlLower.Contains("vi.wikipedia.org"))
                    score += 1000;

                // Vietnamese educational sites
                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 900;
                break;

            case SubjectCategory.Science:
                // Khan Academy, Coursera for theory
                if (urlLower.Contains("khanacademy.org") || urlLower.Contains("coursera.org") || urlLower.Contains("edx.org"))
                    score += 1000;

                // Wikipedia for concepts
                if (urlLower.Contains("wikipedia.org"))
                    score += 900;

                // Vietnamese educational sites
                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 800;
                break;

            case SubjectCategory.Business:
                // Vietnamese news (economics/business)
                if (urlLower.Contains("vnexpress.net") || urlLower.Contains("cafef.vn") || urlLower.Contains("dantri.com.vn"))
                    score += 900;

                // Academic sources
                if (Sources.AcademicSources.Any(site => urlLower.Contains(site)))
                    score += 800;

                // Vietnamese educational sites
                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 700;
                break;
        }

        // Universal: Topic keyword matching
        var topicTokens = topic.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in topicTokens.Where(t => t.Length > 3))
        {
            if (urlLower.Contains(token))
                score += 200;
        }

        // Universal: Official platform docs bonus (if relevant)
        var officialDocs = new[]
        {
            ("developer.android.com", new[] { "android", "mobile", "kotlin" }),
            ("learn.microsoft.com", new[] { "asp.net", "c#", ".net" }),
            ("react.dev", new[] { "react", "javascript" }),
            ("docs.oracle.com/java", new[] { "java" }),
        };

        foreach (var (domain, requiredKeywords) in officialDocs)
        {
            if (urlLower.Contains(domain) && requiredKeywords.Any(kw => technologyKeywords.Contains(kw)))
            {
                score += 300;
            }
        }

        return score;
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
        var topicLower = topic.ToLowerInvariant();

        // Only for programming/CS subjects
        if (category != SubjectCategory.Programming && category != SubjectCategory.ComputerScience)
            return null;

        // Android
        if (technologyKeywords.Contains("android"))
        {
            if (topicLower.Contains("activity"))
                return "https://developer.android.com/guide/components/activities";
            if (topicLower.Contains("layout"))
                return "https://developer.android.com/guide/topics/ui/declaring-layout";
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
}
