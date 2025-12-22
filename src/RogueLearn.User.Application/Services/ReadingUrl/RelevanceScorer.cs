namespace RogueLearn.User.Application.Services;

public static class RelevanceScorer
{
    public static int CalculateRelevanceScore(
        string url,
        string fullResult,
        string topic,
        List<string> technologyKeywords,
        SubjectCategory category)
    {
        var urlLower = url.ToLowerInvariant();
        var resultLower = fullResult.ToLowerInvariant();
        int score = 0;

        switch (category)
        {
            case SubjectCategory.Programming:
            case SubjectCategory.ComputerScience:
                if (Sources.TutorialSites.Any(site => urlLower.Contains(site)))
                    score += 1000;

                if (Sources.CommunityBlogs.Any(site => urlLower.Contains(site)))
                    score += 900;

                // Context match bonus
                foreach (var keyword in technologyKeywords)
                {
                    if (urlLower.Contains(keyword))
                        score += 500;
                    if (resultLower.Contains(keyword))
                        score += 100;
                }

                // --- C & C++ Specific ---
                if (technologyKeywords.Contains("c") || technologyKeywords.Contains("c++"))
                {
                    if (urlLower.Contains("cppreference.com") || urlLower.Contains("cplusplus.com") || urlLower.Contains("learn-c.org"))
                        score += 1500;

                    if (urlLower.Contains("geeksforgeeks.org") && (urlLower.Contains("/c-programming") || urlLower.Contains("/c-plus-plus")))
                        score += 800;

                    if (urlLower.Contains("tutorialspoint.com") && (urlLower.Contains("/cprogramming") || urlLower.Contains("/cplusplus")))
                        score += 800;

                    // Penalty for seeing other languages in snippet if they are not in context
                    if (!technologyKeywords.Contains("python") && resultLower.Contains("python")) score -= 300;
                    if (!technologyKeywords.Contains("java") && resultLower.Contains("java")) score -= 300;
                }

                // --- Java Specific ---
                if (technologyKeywords.Contains("java") && !technologyKeywords.Contains("javascript"))
                {
                    if (urlLower.Contains("/java/") || urlLower.Contains("/servlet/") || urlLower.Contains("/spring/"))
                        score += 600;

                    if (urlLower.Contains("docs.oracle.com") || urlLower.Contains("oracle.com/javase"))
                        score += 800;

                    if (urlLower.Contains("baeldung.com"))
                        score += 700;

                    var topicLower = topic.ToLowerInvariant();
                    if (topicLower.Contains("servlet") && urlLower.Contains("servlet")) score += 500;
                    if (topicLower.Contains("jsp") && urlLower.Contains("jsp")) score += 500;
                }

                // --- C# / .NET Specific ---
                if (technologyKeywords.Contains("c#") || technologyKeywords.Contains(".net"))
                {
                    if (urlLower.Contains("learn.microsoft.com") || urlLower.Contains("docs.microsoft.com"))
                        score += 1000;
                }

                // --- Python Specific ---
                if (technologyKeywords.Contains("python"))
                {
                    if (urlLower.Contains("docs.python.org") || urlLower.Contains("realpython.com"))
                        score += 1000;
                }

                // --- Web Frontend Specific ---
                if (technologyKeywords.Contains("react"))
                {
                    if (urlLower.Contains("react.dev") || urlLower.Contains("legacy.reactjs.org")) score += 1000;
                }
                if (technologyKeywords.Contains("vue"))
                {
                    if (urlLower.Contains("vuejs.org")) score += 1000;
                }
                if (technologyKeywords.Contains("angular"))
                {
                    if (urlLower.Contains("angular.io") || urlLower.Contains("angular.dev")) score += 1000;
                }

                // Block wrong tech patterns on generic sites (Safety Net)
                // This logic ensures a C programming subject doesn't get a Python tutorial from the same site
                var wrongTechPatterns = new[]
                {
                    "w3schools.com/python", "w3schools.com/nodejs", "w3schools.com/php", "w3schools.com/sql", "w3schools.com/java",
                    "programiz.com/cpp", "programiz.com/c-programming", "programiz.com/java", "programiz.com/python"
                };

                foreach (var pattern in wrongTechPatterns)
                {
                    if (urlLower.Contains(pattern))
                    {
                        bool isAllowed = false;
                        if (pattern.Contains("python") && technologyKeywords.Contains("python")) isAllowed = true;
                        if (pattern.Contains("nodejs") && technologyKeywords.Contains("nodejs")) isAllowed = true;
                        if (pattern.Contains("php") && technologyKeywords.Contains("php")) isAllowed = true;
                        if (pattern.Contains("sql") && technologyKeywords.Contains("sql")) isAllowed = true;
                        if (pattern.Contains("java") && !pattern.Contains("javascript") && technologyKeywords.Contains("java")) isAllowed = true;
                        if (pattern.Contains("cpp") && technologyKeywords.Contains("c++")) isAllowed = true;
                        if (pattern.Contains("/c-programming") && technologyKeywords.Contains("c")) isAllowed = true;

                        if (!isAllowed)
                        {
                            return -1000; // Heavily penalize mismatch
                        }
                    }
                }
                break;

            case SubjectCategory.VietnamesePolitics:
                if (urlLower.Contains("dangcongsan.vn") || urlLower.Contains("chinhphu.vn"))
                    score += 1500;

                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 1200;

                if (urlLower.Contains("vnexpress.net") || urlLower.Contains("nhandan.vn") || urlLower.Contains("thanhnien.vn"))
                    score += 800;

                if (urlLower.Contains("vi.wikipedia.org"))
                    score += 700;
                break;

            case SubjectCategory.History:
                if (urlLower.Contains("wikipedia.org"))
                    score += 1000;

                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 900;

                if (Sources.AcademicSources.Any(site => urlLower.Contains(site)))
                    score += 800;
                break;

            case SubjectCategory.VietnameseLiterature:
                if (urlLower.Contains("vietjack.com"))
                    score += 1200;

                if (urlLower.Contains("vi.wikipedia.org"))
                    score += 1000;

                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 900;
                break;

            case SubjectCategory.Science:
                if (urlLower.Contains("khanacademy.org") || urlLower.Contains("coursera.org") || urlLower.Contains("edx.org"))
                    score += 1000;

                if (urlLower.Contains("wikipedia.org"))
                    score += 900;

                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 800;
                break;

            case SubjectCategory.Business:
                if (urlLower.Contains("vnexpress.net") || urlLower.Contains("cafef.vn") || urlLower.Contains("dantri.com.vn"))
                    score += 900;

                if (Sources.AcademicSources.Any(site => urlLower.Contains(site)))
                    score += 800;

                if (Sources.VietnameseEducationalSites.Any(site => urlLower.Contains(site)))
                    score += 700;
                break;
        }

        var topicTokens = topic.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in topicTokens.Where(t => t.Length > 3))
        {
            if (urlLower.Contains(token))
                score += 200;
        }

        // Boost official docs for other frameworks not covered in detail above
        var officialDocs = new[]
        {
            ("developer.android.com", new[] { "android", "mobile", "kotlin" }),
            ("learn.microsoft.com", new[] { "asp.net", "c#", ".net" }),
            ("react.dev", new[] { "react", "javascript" }),
            ("docs.oracle.com/java", new[] { "java" }),
            ("flutter.dev", new[] { "flutter", "dart" }),
            ("developer.mozilla.org", new[] { "javascript", "html", "css" }),
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
}