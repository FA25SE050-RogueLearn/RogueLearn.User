// RogueLearn.User/src/RogueLearn.User.Application/Services/ReadingUrl/Sources.cs
namespace RogueLearn.User.Application.Services;

public static class Sources
{
    public static readonly string[] TutorialSites = new[]
    {
        "geeksforgeeks.org",
        "w3schools.com",
        "tutorialspoint.com",
        "programiz.com",
        "javatpoint.com",
        "tutorialsteacher.com",
        "guru99.com",
        "studytonight.com",
        "baeldung.com",
        "jenkov.com",
        "mkyong.com",
        "viblo.asia",
        "topdev.vn",
        "200lab.io",
        "techtalk.vn",
        "techmaster.vn",
    };

    public static readonly string[] CommunityBlogs = new[]
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
        "kentcdodds.com",
        "joshwcomeau.com",
        "overreacted.io",
        "dan.luu",
        "pragmaticengineer.com",
        "martinfowler.com",
    };

    public static readonly string[] VietnameseEducationalSites = new[]
    {
        "vietjack.com",
        "tailieu.vn",
        "123doc.net",
        "hocmai.vn",
        "tuyensinh247.com",
        "loigiaihay.com",
        "thuvienphapluat.vn",
        "dangcongsan.vn",
        "chinhphu.vn",
        "nhandan.vn",
        "vnexpress.net",
        "thanhnien.vn",
        "tuoitre.vn",
        "dantri.com.vn",
        "baomoi.com",
        "cafef.vn",
        "vi.wikipedia.org",
    };

    public static readonly string[] AcademicSources = new[]
    {
        "wikipedia.org",
        "britannica.com",
        "khanacademy.org",
        "coursera.org",
        "edx.org",
        "mit.edu",
        "stanford.edu",
    };

    public static readonly string[] UntrustedSourcesForProgramming = new[]
    {
        "reddit.com",
        "stackoverflow.com/questions",
        "quora.com",
        "forum.freecodecamp.org",
        "discuss.codecademy.com",
        "answers.unity.com",
        "community.atlassian.com", // ⭐ ADDED: Block Jira/Atlassian community threads
        "/forum/",
        "/forums/",
        "/discussion/",
        "/community/t/",
        "/community/questions/",
        "medium.com",
        "scribd.com",
        "slideshare.net",
        "academia.edu",
        "coursera.org",
        "udemy.com",
        "researchgate.net",
        "arxiv.org",
        "scholar.google",
        "ieee.org",
        "acm.org",
        "/paper/",
        "/papers/",
        "/research/",
        "youtube.com",
        "vimeo.com",
    };

    public static readonly string[] UniversalBlockedSources = new[]
    {
        "scribd.com",
        "slideshare.net",
        ".pdf",
        ".ppt",
        ".pptx",
        ".doc",
        ".docx",
        ".zip",
    };
}