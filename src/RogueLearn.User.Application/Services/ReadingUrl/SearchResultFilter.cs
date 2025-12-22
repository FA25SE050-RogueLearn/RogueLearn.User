using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Services;

public static class SearchResultFilter
{
    public static List<string> FilterAndPrioritizeResults(
        IEnumerable<string> searchResults,
        string topic,
        List<string> technologyKeywords,
        SubjectCategory category,
        ILogger logger)
    {
        var scoredUrls = new List<(string Url, int Score)>();

        foreach (var result in searchResults)
        {
            var url = SearchResultParser.ExtractUrlFromSearchResult(result);
            if (string.IsNullOrWhiteSpace(url)) continue;

            if (IsUntrustedSource(url, category, logger))
            {
                logger.LogDebug("  → 🚫 BLOCKED (untrusted source): {Url}", url);
                continue;
            }

            if ((category == SubjectCategory.Programming || category == SubjectCategory.ComputerScience) &&
                IsWrongFramework(url, technologyKeywords, category, topic, logger))
            {
                logger.LogDebug("  → 🚫 BLOCKED (wrong framework/content): {Url}", url);
                continue;
            }

            int score = RelevanceScorer.CalculateRelevanceScore(url, result, topic, technologyKeywords, category);

            if (score > 0)
            {
                scoredUrls.Add((url, score));
                logger.LogDebug("  → {Score} pts: {Url}", score, url);
            }
            else
            {
                logger.LogDebug("  → ❌ BLOCKED (low relevance): {Url}", url);
            }
        }

        return scoredUrls
            .OrderByDescending(x => x.Score)
            .Select(x => x.Url)
            .ToList();
    }

    public static bool IsUntrustedSource(string url, SubjectCategory category, ILogger logger)
    {
        var urlLower = url.ToLowerInvariant();

        foreach (var blocked in Sources.UniversalBlockedSources)
        {
            if (urlLower.Contains(blocked))
            {
                logger.LogDebug("🚫 Universal block: {Pattern}", blocked);
                return true;
            }
        }

        switch (category)
        {
            case SubjectCategory.Programming:
            case SubjectCategory.ComputerScience:
                foreach (var untrusted in Sources.UntrustedSourcesForProgramming)
                {
                    if (urlLower.Contains(untrusted))
                    {
                        logger.LogDebug("🚫 Programming/CS-specific block: {Pattern}", untrusted);
                        return true;
                    }
                }
                break;

            case SubjectCategory.VietnamesePolitics:
            case SubjectCategory.History:
            case SubjectCategory.VietnameseLiterature:
                return false;

            case SubjectCategory.Science:
            case SubjectCategory.Business:
                return false;

            default:
                return false;
        }

        return false;
    }

    public static bool IsWrongFramework(string url, List<string> technologyKeywords, SubjectCategory category, string topic, ILogger logger)
    {
        var urlLower = url.ToLowerInvariant();
        var uri = new Uri(url);
        var path = uri.AbsolutePath.ToLowerInvariant();

        if (category == SubjectCategory.Programming || category == SubjectCategory.ComputerScience)
        {
            // 1. C & C++ Strict Filtering
            if (technologyKeywords.Contains("c") || technologyKeywords.Contains("c++"))
            {
                // Block Python
                if (!technologyKeywords.Contains("python") && (path.Contains("/python/") || path.Contains("/py/") || urlLower.Contains("pypi.org") || urlLower.Contains("docs.python.org")))
                {
                    logger.LogDebug("❌ Wrong language: Python path detected in C/C++ context");
                    return true;
                }

                // Block Java
                if (!technologyKeywords.Contains("java") && (path.Contains("/java/") || urlLower.Contains("baeldung.com") || urlLower.Contains("docs.oracle.com")))
                {
                    logger.LogDebug("❌ Wrong language: Java path detected in C/C++ context");
                    return true;
                }

                // Block .NET/C#
                if (!technologyKeywords.Contains("c#") && !technologyKeywords.Contains(".net") &&
                   (path.Contains("/csharp/") || path.Contains("/dotnet/") || path.Contains("/aspnet/") || path.Contains("learn.microsoft.com")))
                {
                    logger.LogDebug("❌ Wrong language: C#/.NET path detected in C/C++ context");
                    return true;
                }

                // Block JS/Web
                if (!technologyKeywords.Contains("javascript") &&
                   (path.Contains("/javascript/") || path.Contains("/js/") || path.Contains("/react/") || path.Contains("/angular/") || path.Contains("/vue/")))
                {
                    logger.LogDebug("❌ Wrong language: JS/Web path detected in C/C++ context");
                    return true;
                }

                // W3Schools/Programiz section check
                if (urlLower.Contains("w3schools.com") && !path.Contains("/c/") && !path.Contains("/cpp/"))
                {
                    logger.LogDebug("❌ Wrong W3Schools section for C/C++");
                    return true;
                }
                if (urlLower.Contains("programiz.com") && !path.Contains("/c-programming") && !path.Contains("/cpp-programming"))
                {
                    logger.LogDebug("❌ Wrong Programiz section for C/C++");
                    return true;
                }
            }

            // 2. Java Strict Filtering
            if (technologyKeywords.Contains("java") && !technologyKeywords.Contains("javascript"))
            {
                var dotNetPaths = new[] { "/asp/", "/aspnet/", "/dotnet/", "/csharp/", "/cs/", "/asp.net/", "/c-sharp/", "/vb.net/" };
                if (dotNetPaths.Any(pattern => path.Contains(pattern)))
                {
                    logger.LogDebug("❌ Wrong language path: .NET/C# (expected Java)");
                    return true;
                }

                var jsPaths = new[] { "/javascript/", "/js/", "/react/", "/angular/", "/vue/" };
                if (jsPaths.Any(pattern => path.Contains(pattern)))
                {
                    logger.LogDebug("❌ Wrong language path: JavaScript/Web (expected Java)");
                    return true;
                }

                if (urlLower.Contains("w3schools.com") && !path.Contains("/java/"))
                {
                    logger.LogDebug("❌ Wrong W3Schools section (expected Java)");
                    return true;
                }
            }

            // 3. C# / .NET Strict Filtering
            if (technologyKeywords.Contains("c#") || technologyKeywords.Contains(".net") || technologyKeywords.Contains("asp.net"))
            {
                var javaPaths = new[] { "/java/", "/jsp/", "/servlet/", "/spring/", "/jakarta/" };
                if (javaPaths.Any(pattern => path.Contains(pattern)))
                {
                    logger.LogDebug("❌ Wrong language path: Java (expected C#/.NET)");
                    return true;
                }

                if (urlLower.Contains("docs.oracle.com"))
                {
                    logger.LogDebug("❌ Wrong doc site: Oracle (expected Microsoft)");
                    return true;
                }
            }

            // 4. Python Strict Filtering
            if (technologyKeywords.Contains("python"))
            {
                var otherLangPaths = new[] { "/java/", "/csharp/", "/cpp/", "/golang/", "/ruby/" };
                if (otherLangPaths.Any(pattern => path.Contains(pattern)))
                {
                    logger.LogDebug("❌ Wrong language path (expected Python)");
                    return true;
                }
            }

            // 5. Mobile (Android/Kotlin/Flutter) Filtering
            if (technologyKeywords.Contains("android") || technologyKeywords.Contains("kotlin"))
            {
                if (!technologyKeywords.Contains("flutter") && (urlLower.Contains("flutter") || path.Contains("/flutter/")))
                {
                    logger.LogDebug("❌ Wrong framework: Flutter (expected Native Android)");
                    return true;
                }
                if (!technologyKeywords.Contains("react") && (urlLower.Contains("react-native") || urlLower.Contains("reactnative")))
                {
                    logger.LogDebug("❌ Wrong framework: React Native (expected Native Android)");
                    return true;
                }
            }

            if (technologyKeywords.Contains("flutter"))
            {
                if (!technologyKeywords.Contains("kotlin") && !technologyKeywords.Contains("java") && (path.Contains("/android/") || path.Contains("/kotlin/")))
                {
                    // Allow if it's explicitly comparing, otherwise block native docs
                    if (!urlLower.Contains("vs") && !urlLower.Contains("comparison"))
                    {
                        logger.LogDebug("❌ Wrong framework: Native Android (expected Flutter)");
                        return true;
                    }
                }
            }

            // 6. SQL Filtering
            if (technologyKeywords.Contains("sql") || technologyKeywords.Contains("database"))
            {
                // Avoid NoSQL if strictly SQL context
                if (!technologyKeywords.Contains("nosql") && !technologyKeywords.Contains("mongo") &&
                   (path.Contains("/mongodb/") || path.Contains("/dynamodb/") || urlLower.Contains("mongoose")))
                {
                    logger.LogDebug("❌ Wrong DB type: NoSQL (expected SQL)");
                    return true;
                }
            }

            // 7. Web Frontend (React/Vue/Angular)
            if (technologyKeywords.Contains("react"))
            {
                if (!technologyKeywords.Contains("angular") && (path.Contains("/angular/") || urlLower.Contains("angular.io")))
                {
                    logger.LogDebug("❌ Wrong framework: Angular (expected React)");
                    return true;
                }
                if (!technologyKeywords.Contains("vue") && (path.Contains("/vue/") || urlLower.Contains("vuejs.org")))
                {
                    logger.LogDebug("❌ Wrong framework: Vue (expected React)");
                    return true;
                }
            }

            if (technologyKeywords.Contains("vue"))
            {
                if (path.Contains("/react/") || path.Contains("/angular/"))
                {
                    logger.LogDebug("❌ Wrong framework (expected Vue)");
                    return true;
                }
            }

            if (technologyKeywords.Contains("angular"))
            {
                if (path.Contains("/react/") || path.Contains("/vue/"))
                {
                    logger.LogDebug("❌ Wrong framework (expected Angular)");
                    return true;
                }
            }
        }

        return false;
    }
}