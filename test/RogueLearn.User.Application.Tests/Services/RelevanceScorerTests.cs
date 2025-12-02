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
}