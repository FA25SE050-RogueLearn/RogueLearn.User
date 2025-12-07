using FluentAssertions;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class RelevanceScorerTests
{
    [Fact]
    public void CalculateRelevanceScore_C_Context_TutorialSite_Positive()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://www.geeksforgeeks.org/c-programming-language/",
            "Title: C Programming Language\nLink: https://www.geeksforgeeks.org/c-programming-language/",
            "C pointers",
            new List<string> { "c" },
            SubjectCategory.Programming);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_C_Context_Python_Link_Negative()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://www.w3schools.com/python/python_tuples.asp",
            "Title: Python Tuples\nLink: https://www.w3schools.com/python/python_tuples.asp",
            "C arrays",
            new List<string> { "c" },
            SubjectCategory.Programming);
        score.Should().BeLessThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_C_Context_TutorialsPoint_Positive()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://www.tutorialspoint.com/cprogramming/index.htm",
            "Link: https://www.tutorialspoint.com/cprogramming/index.htm",
            "C basics",
            new List<string> { "c" },
            SubjectCategory.Programming);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_Java_Baeldung_Positive()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://www.baeldung.com/java-collections",
            "Link: https://www.baeldung.com/java-collections",
            "Java collections",
            new List<string> { "java" },
            SubjectCategory.Programming);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_WebFrameworks_Positive()
    {
        var react = RelevanceScorer.CalculateRelevanceScore(
            "https://react.dev/learn",
            "Link: https://react.dev/learn",
            "React basics",
            new List<string> { "react" },
            SubjectCategory.Programming);
        var vue = RelevanceScorer.CalculateRelevanceScore(
            "https://vuejs.org/guide/introduction.html",
            "Link: https://vuejs.org/guide/introduction.html",
            "Vue basics",
            new List<string> { "vue" },
            SubjectCategory.Programming);
        var angular = RelevanceScorer.CalculateRelevanceScore(
            "https://angular.io/guide/components",
            "Link: https://angular.io/guide/components",
            "Angular components",
            new List<string> { "angular" },
            SubjectCategory.Programming);
        react.Should().BeGreaterThan(0);
        vue.Should().BeGreaterThan(0);
        angular.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_TopicToken_Match_Bonus()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://example.com/cache-memory-guide",
            "Link: https://example.com/cache-memory-guide",
            "Cache memory",
            new List<string> { },
            SubjectCategory.ComputerScience);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_Categories_Positive()
    {
        var hist = RelevanceScorer.CalculateRelevanceScore(
            "https://en.wikipedia.org/wiki/Vietnam",
            "Link: https://en.wikipedia.org/wiki/Vietnam",
            "Vietnam history",
            new List<string> { },
            SubjectCategory.History);
        var sci = RelevanceScorer.CalculateRelevanceScore(
            "https://www.khanacademy.org/math",
            "Link: https://www.khanacademy.org/math",
            "Calculus",
            new List<string> { },
            SubjectCategory.Science);
        var lit = RelevanceScorer.CalculateRelevanceScore(
            "https://vietjack.com/ngu_van/",
            "Link: https://vietjack.com/ngu_van/",
            "Ngữ văn",
            new List<string> { },
            SubjectCategory.VietnameseLiterature);
        hist.Should().BeGreaterThan(0);
        sci.Should().BeGreaterThan(0);
        lit.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_VietnamesePolitics_Positive_For_Gov_Site()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://chinhphu.vn/news",
            "Link: https://chinhphu.vn/news",
            "Chính trị",
            new List<string> { },
            SubjectCategory.VietnamesePolitics);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_Business_Positive_For_Cafef()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://cafef.vn/article",
            "Link: https://cafef.vn/article",
            "Kinh tế",
            new List<string> { },
            SubjectCategory.Business);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_OfficialDocsBoost_For_DotNet()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://learn.microsoft.com/en-us/dotnet/csharp/",
            "Link: https://learn.microsoft.com/en-us/dotnet/csharp/",
            "C# basics",
            new List<string> { "c#" },
            SubjectCategory.Programming);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_OfficialDocsBoost_For_Android()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://developer.android.com/guide",
            "Link: https://developer.android.com/guide",
            "Activities",
            new List<string> { "android" },
            SubjectCategory.Programming);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_WrongTechPattern_Penalty()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://www.w3schools.com/python/python_tuples.asp",
            "Link: https://www.w3schools.com/python/python_tuples.asp",
            "C arrays",
            new List<string> { "c" },
            SubjectCategory.Programming);
        score.Should().BeLessThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_WrongTechPattern_Programiz_Java_Penalty()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://www.programiz.com/java-programming",
            "Link: https://www.programiz.com/java-programming",
            "C arrays",
            new List<string> { "c" },
            SubjectCategory.Programming);
        score.Should().BeLessThan(0);
    }

    [Fact]
    public void CalculateRelevanceScore_Python_RealPython_Positive()
    {
        var score = RelevanceScorer.CalculateRelevanceScore(
            "https://realpython.com/",
            "Link: https://realpython.com/",
            "Python basics",
            new List<string> { "python" },
            SubjectCategory.Programming);
        score.Should().BeGreaterThan(0);
    }
}
