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

        if (category == SubjectCategory.Programming)
        {
            if (technologyKeywords.Contains("java") && !technologyKeywords.Contains("javascript"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                var dotNetPaths = new[] {
                    "/asp/", "/aspnet/", "/dotnet/", "/csharp/", "/cs/",
                    "/asp.net/", "/c-sharp/", "/vb.net/"
                };

                if (dotNetPaths.Any(pattern => path.Contains(pattern)))
                {
                    logger.LogDebug("❌ Wrong language path: .NET/C# (expected Java) - Path: {Path}", path);
                    return true;
                }

                if (urlLower.Contains("w3schools.com") &&
                    (path.Contains("/asp/") || path.Contains("/cs/")))
                {
                    logger.LogDebug("❌ W3Schools .NET page (expected Java)");
                    return true;
                }

                var phpPaths = new[] { "/php/", "/php5/", "/php7/" };
                if (phpPaths.Any(pattern => path.Contains(pattern)))
                {
                    logger.LogDebug("❌ Wrong language: PHP (expected Java)");
                    return true;
                }

                var topicLower = topic.ToLowerInvariant();
                var isPythonPath = path.Contains("/python/") || path.Contains("/py/");
                var isComparisonTopic = topicLower.Contains("comparison") || topicLower.Contains("vs") ||
                                       topicLower.Contains("difference");

                if (isPythonPath && !isComparisonTopic)
                {
                    logger.LogDebug("❌ Wrong language: Python tutorial (expected Java)");
                    return true;
                }
            }

            if (technologyKeywords.Contains("asp.net") || technologyKeywords.Contains("c#") || technologyKeywords.Contains(".net"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                var javaPaths = new[] {
                    "/java/", "/jsp/", "/servlet/", "/spring/",
                    "/java-ee/", "/jakarta/"
                };

                if (javaPaths.Any(pattern => path.Contains(pattern)))
                {
                    logger.LogDebug("❌ Wrong language path: Java (expected ASP.NET/C#) - Path: {Path}", path);
                    return true;
                }
            }

            if (technologyKeywords.Contains("android") || technologyKeywords.Contains("kotlin") || technologyKeywords.Contains("mobile"))
            {
                if (urlLower.Contains("flutter") || urlLower.Contains("/flutter/"))
                {
                    logger.LogDebug("❌ Wrong framework: Flutter (expected Android)");
                    return true;
                }

                if (urlLower.Contains("react-native") || urlLower.Contains("reactnative"))
                {
                    logger.LogDebug("❌ Wrong framework: React Native (expected Android)");
                    return true;
                }

                if (urlLower.Contains("xamarin"))
                {
                    logger.LogDebug("❌ Wrong framework: Xamarin (expected Android)");
                    return true;
                }

                if (urlLower.Contains("localstorage") || urlLower.Contains("local-storage") || urlLower.Contains("sessionstorage"))
                {
                    logger.LogDebug("❌ Web content (expected Android native)");
                    return true;
                }

                if (urlLower.Contains("chrome-extension") || urlLower.Contains("chrome/extension"))
                {
                    logger.LogDebug("❌ Chrome extension content (expected Android)");
                    return true;
                }
            }

            if (technologyKeywords.Contains("react"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                if (path.Contains("/angular/") || path.Contains("/vue/"))
                {
                    logger.LogDebug("❌ Wrong framework (expected React)");
                    return true;
                }
            }

            if (technologyKeywords.Contains("vue"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                if (path.Contains("/react/") || path.Contains("/angular/"))
                {
                    logger.LogDebug("❌ Wrong framework (expected Vue)");
                    return true;
                }
            }

            if (technologyKeywords.Contains("angular"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                if (path.Contains("/react/") || path.Contains("/vue/"))
                {
                    logger.LogDebug("❌ Wrong framework (expected Angular)");
                    return true;
                }
            }

            if (technologyKeywords.Contains("python"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.ToLowerInvariant();

                var otherLanguagePaths = new[] { "/java/", "/csharp/", "/asp/", "/dotnet/" };
                if (otherLanguagePaths.Any(pattern => path.Contains(pattern)))
                {
                    logger.LogDebug("❌ Wrong language (expected Python)");
                    return true;
                }
            }
        }

        if (category == SubjectCategory.ComputerScience)
        {
            var topicLower = topic.ToLowerInvariant();

            if (urlLower.Contains("w3schools.com"))
            {
                if (topicLower.Contains("assembly") || topicLower.Contains("architecture") ||
                    topicLower.Contains("operating system") || topicLower.Contains("computer organization"))
                {
                    var webDevSections = new[] { "/html/", "/css/", "/js/", "/javascript/", "/react/", "/vue/", "/angular/", "/bootstrap/" };
                    if (webDevSections.Any(section => urlLower.Contains(section)))
                    {
                        logger.LogDebug("❌ Wrong W3Schools section: {Url} (expected CS theory, got web dev)", url);
                        return true;
                    }
                }

                if (topicLower.Contains("data structure") || topicLower.Contains("algorithm"))
                {
                    var webDevSections = new[] { "/html/", "/css/", "/js/", "/javascript/", "/react/", "/vue/", "/sql/" };
                    if (webDevSections.Any(section => urlLower.Contains(section)))
                    {
                        logger.LogDebug("❌ Wrong W3Schools section: {Url} (expected DS/Algo, got web dev)", url);
                        return true;
                    }
                }
            }

            if (urlLower.Contains("tutorialspoint.com"))
            {
                if (topicLower.Contains("assembly") || topicLower.Contains("architecture") ||
                    topicLower.Contains("operating system") || topicLower.Contains("computer organization"))
                {
                    var webDevSections = new[] { "/html/", "/css/", "/javascript/", "/reactjs/", "/vuejs/", "/angular/" };
                    if (webDevSections.Any(section => urlLower.Contains(section)))
                    {
                        logger.LogDebug("❌ Wrong TutorialsPoint section: {Url} (expected CS theory, got web dev)", url);
                        return true;
                    }
                }
            }

            if (urlLower.Contains("geeksforgeeks.org"))
            {
                if (topicLower.Contains("assembly") || topicLower.Contains("architecture") ||
                    topicLower.Contains("operating system"))
                {
                    var webDevKeywords = new[] { "-html-", "-css-", "-javascript-", "-react-", "-vue-" };
                    if (webDevKeywords.Any(keyword => urlLower.Contains(keyword)))
                    {
                        logger.LogDebug("❌ Wrong GeeksforGeeks article: {Url} (expected CS theory, got web dev)", url);
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
