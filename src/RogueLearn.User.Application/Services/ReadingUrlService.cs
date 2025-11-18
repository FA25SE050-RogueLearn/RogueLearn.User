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

    // HIGH PRIORITY: Tutorial sites with clear examples (PROGRAMMING)
    private static readonly string[] TutorialSites = new[]
    {
        "geeksforgeeks.org",
        "w3schools.com",
        "tutorialspoint.com",
        "programiz.com",
        "javatpoint.com",
        "tutorialsteacher.com",
        "guru99.com",
        "studytonight.com",
        
        // Vietnamese programming
        "viblo.asia",
        "topdev.vn",
        "200lab.io",
        "techtalk.vn",
        "techmaster.vn",
    };

    // HIGH PRIORITY: Community blogs (PROGRAMMING)
    private static readonly string[] CommunityBlogs = new[]
    {
        "dev.to",
        "hashnode.dev",
        "freecodecamp.org",
        "digitalocean.com/community",
        "css-tricks.com",
        "smashingmagazine.com",
        "logrocket.com/blog",
        "scotch.io",
        "sitepoint.com",
        
        // Personal blogs
        "kentcdodds.com",
        "joshwcomeau.com",
        "overreacted.io",
        "dan.luu",
        "pragmaticengineer.com",
        "martinfowler.com",
    };

    // HIGH PRIORITY: Vietnamese educational sites (NON-PROGRAMMING)
    private static readonly string[] VietnameseEducationalSites = new[]
    {
        // Educational/Quiz sites
        "vietjack.com",          // Quizzes, exercises, theory
        "tailieu.vn",            // Study materials
        "123doc.net",            // Documents
        "hocmai.vn",             // Online learning
        "tuyensinh247.com",      // Educational content
        "loigiaihay.com",        // Solutions and explanations
        
        // Political/Ideological (for Marxism, HCM Thought)
        "thuvienphapluat.vn",    // Legal/political documents
        "dangcongsan.vn",        // Communist Party official site
        "chinhphu.vn",           // Government official site
        "nhandan.vn",            // Official news (theory articles)
        
        // News/Articles (for current events, politics, economics)
        "vnexpress.net",
        "thanhnien.vn",
        "tuoitre.vn",
        "dantri.com.vn",
        "baomoi.com",
        "cafef.vn",              // Business/economics
        
        // Wikipedia Vietnamese
        "vi.wikipedia.org",
    };

    // HIGH PRIORITY: Academic sources (THEORY/SCIENCE)
    private static readonly string[] AcademicSources = new[]
    {
        "wikipedia.org",
        "britannica.com",
        "khanacademy.org",
        "coursera.org",
        "edx.org",
        "mit.edu",
        "stanford.edu",
    };

    // BLOCKED: Untrusted sources for PROGRAMMING subjects
    private static readonly string[] UntrustedSourcesForProgramming = new[]
    {
        // Forums and discussion platforms (not tutorials)
        "reddit.com",
        "stackoverflow.com/questions",
        "quora.com",
        "forum.freecodecamp.org",
        "discuss.codecademy.com",
        "answers.unity.com",
        "/forum/",
        "/forums/",
        "/discussion/",
        "/community/t/",
        "/community/questions/",
        
        // Paywalled/Login-required content
        "medium.com",
        "scribd.com",
        "slideshare.net",
        "academia.edu",
        "coursera.org",
        "udemy.com",
        
        // Academic/Research (too dense for practical learning)
        "researchgate.net",
        "arxiv.org",
        "scholar.google",
        "ieee.org",
        "acm.org",
        "/paper/",
        "/papers/",
        "/research/",
        
        // Video platforms (prefer text tutorials)
        "youtube.com",
        "vimeo.com",
    };

    // BLOCKED: Untrusted sources for ALL subjects (paywalls only)
    private static readonly string[] UniversalBlockedSources = new[]
    {
        "scribd.com",
        "slideshare.net",
        ".pdf",  // Direct PDF downloads
        ".ppt",
        ".pptx",
        ".doc",
        ".docx",
        ".zip",
    };

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

        // Extract technology keywords from context
        var technologyKeywords = ExtractTechnologyKeywords(subjectContext);
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
            var searchQuery = BuildContextAwareQuery(topic, subjectContext, category);
            _logger.LogDebug("Search query: '{Query}'", searchQuery);

            var searchResults = await _webSearchService.SearchAsync(
                searchQuery, count: 15, offset: 0, cancellationToken);

            if (searchResults == null || !searchResults.Any())
            {
                _logger.LogWarning("No search results returned");
                return null;
            }

            _logger.LogDebug("Found {Count} raw results, filtering for relevance...", searchResults.Count());

            // Filter and prioritize results
            var relevantUrls = FilterAndPrioritizeResults(searchResults, topic, technologyKeywords, category);

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
        var officialDocUrl = GetOfficialDocumentationUrl(topic, technologyKeywords, category);
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
        if (contextLower.Contains("java") && !contextLower.Contains("javascript")) keywords.Add("java");

        // Web - Backend
        if (contextLower.Contains("asp.net") || contextLower.Contains("aspnet")) keywords.Add("asp.net");
        if (contextLower.Contains("c#") || contextLower.Contains("csharp")) keywords.Add("c#");
        if (contextLower.Contains(".net")) keywords.Add(".net");
        if (contextLower.Contains("node")) keywords.Add("nodejs");
        if (contextLower.Contains("express")) keywords.Add("express");

        // Web - Frontend
        if (contextLower.Contains("react")) keywords.Add("react");
        if (contextLower.Contains("vue")) keywords.Add("vue");
        if (contextLower.Contains("angular")) keywords.Add("angular");
        if (contextLower.Contains("javascript")) keywords.Add("javascript");
        if (contextLower.Contains("typescript")) keywords.Add("typescript");

        // Other
        if (contextLower.Contains("python")) keywords.Add("python");
        if (contextLower.Contains("spring")) keywords.Add("spring");
        if (contextLower.Contains("flutter")) keywords.Add("flutter");
        if (contextLower.Contains("ios") || contextLower.Contains("swift")) keywords.Add("ios");

        return keywords.Distinct().ToList();
    }

    /// <summary>
    /// Build search query with subject context and category awareness.
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
                return $"{topic} tài liệu học tập";
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
            var url = ExtractUrlFromSearchResult(result);
            if (string.IsNullOrWhiteSpace(url)) continue;

            // ⭐ FILTER 1: Block category-specific untrusted sources
            if (IsUntrustedSource(url, category))
            {
                _logger.LogDebug("  → 🚫 BLOCKED (untrusted source): {Url}", url);
                continue;
            }

            // ⭐ FILTER 2: Block wrong frameworks (PROGRAMMING only)
            if (category == SubjectCategory.Programming && IsWrongFramework(url, technologyKeywords))
            {
                _logger.LogDebug("  → 🚫 BLOCKED (wrong framework): {Url}", url);
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
        foreach (var blocked in UniversalBlockedSources)
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
                // For programming: Block forums, discussions, etc.
                foreach (var untrusted in UntrustedSourcesForProgramming)
                {
                    if (urlLower.Contains(untrusted))
                    {
                        _logger.LogDebug("🚫 Programming-specific block: {Pattern}", untrusted);
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
    /// Check if URL is about the wrong technology/framework (PROGRAMMING only).
    /// </summary>
    private bool IsWrongFramework(string url, List<string> technologyKeywords)
    {
        var urlLower = url.ToLowerInvariant();

        // If subject is Android (Kotlin/Java), block Flutter and web-specific content
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

        // If subject is React, block Angular/Vue
        if (technologyKeywords.Contains("react"))
        {
            if (urlLower.Contains("angular") || urlLower.Contains("vue.js") || urlLower.Contains("vuejs"))
            {
                _logger.LogDebug("❌ Wrong framework (expected React)");
                return true;
            }
        }

        // If subject is ASP.NET, block Spring/Django/Express
        if (technologyKeywords.Contains("asp.net") || technologyKeywords.Contains("c#"))
        {
            if (urlLower.Contains("spring-boot") || urlLower.Contains("django") ||
                urlLower.Contains("express.js") || urlLower.Contains("flask"))
            {
                _logger.LogDebug("❌ Wrong framework (expected ASP.NET)");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculate relevance score (CATEGORY-AWARE).
    /// Higher score = more relevant. Score ≤ 0 = BLOCKED.
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
                if (TutorialSites.Any(site => urlLower.Contains(site)))
                    score += 1000;

                // Community blogs
                if (CommunityBlogs.Any(site => urlLower.Contains(site)))
                    score += 900;

                // Technology keyword matches
                foreach (var keyword in technologyKeywords)
                {
                    if (urlLower.Contains(keyword))
                        score += 500;
                    if (resultLower.Contains(keyword))
                        score += 100;
                }

                // Block wrong technology tutorials
                var wrongTechPatterns = new[]
                {
                    "w3schools.com/python", "w3schools.com/nodejs",
                    "w3schools.com/html", "w3schools.com/css",
                    "w3schools.com/sql", "programiz.com/cpp",
                    "programiz.com/c-programming", "programiz.com/python",
                };

                if (wrongTechPatterns.Any(pattern => urlLower.Contains(pattern)))
                    return -500;  // Block
                break;

            case SubjectCategory.VietnamesePolitics:
                // Official government/party sources = HIGHEST priority
                if (urlLower.Contains("dangcongsan.vn") || urlLower.Contains("chinhphu.vn"))
                    score += 1500;

                // Vietnamese educational sites
                if (VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
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
                if (VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 900;

                // Academic sources
                if (AcademicSources.Any(site => urlLower.Contains(site)))
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
                if (VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
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
                if (VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 800;
                break;

            case SubjectCategory.Business:
                // Vietnamese news (economics/business)
                if (urlLower.Contains("vnexpress.net") || urlLower.Contains("cafef.vn") || urlLower.Contains("dantri.com.vn"))
                    score += 900;

                // Academic sources
                if (AcademicSources.Any(site => urlLower.Contains(site)))
                    score += 800;

                // Vietnamese educational sites
                if (VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
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

        // Only for programming subjects
        if (category != SubjectCategory.Programming)
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
