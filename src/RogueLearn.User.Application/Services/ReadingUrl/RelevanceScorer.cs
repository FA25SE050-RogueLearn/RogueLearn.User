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
                if (Sources.TutorialSites.Any(site => urlLower.Contains(site)))
                    score += 1000;

                if (Sources.CommunityBlogs.Any(site => urlLower.Contains(site)))
                    score += 900;

                foreach (var keyword in technologyKeywords)
                {
                    if (urlLower.Contains(keyword))
                        score += 500;
                    if (resultLower.Contains(keyword))
                        score += 100;
                }

                if (technologyKeywords.Contains("java") || technologyKeywords.Contains("java-web"))
                {
                    if (urlLower.Contains("/java/") || urlLower.Contains("/servlet/") ||
                        urlLower.Contains("/jsp/") || urlLower.Contains("java-"))
                        score += 600;

                    if (urlLower.Contains("docs.oracle.com") || urlLower.Contains("oracle.com/javase"))
                        score += 800;

                    if (urlLower.Contains("baeldung.com"))
                        score += 700;

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

                var wrongTechPatterns = new[]
                {
                    "w3schools.com/python", "w3schools.com/nodejs",
                    "w3schools.com/php", "w3schools.com/sql",
                    "programiz.com/cpp", "programiz.com/c-programming"
                };

                if (wrongTechPatterns.Any(pattern => urlLower.Contains(pattern)))
                {
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
                if (Sources.TutorialSites.Any(site => urlLower.Contains(site)))
                    score += 1000;

                if (Sources.AcademicSources.Any(site => urlLower.Contains(site)))
                    score += 900;

                if (Sources.CommunityBlogs.Any(site => urlLower.Contains(site)))
                    score += 800;

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

                var csTopicLower = topic.ToLowerInvariant();
                if (csTopicLower.Contains("assembly") && urlLower.Contains("assembly"))
                    score += 500;
                if (csTopicLower.Contains("architecture") && urlLower.Contains("architecture"))
                    score += 500;
                if (csTopicLower.Contains("operating") && urlLower.Contains("operating"))
                    score += 500;

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
}
