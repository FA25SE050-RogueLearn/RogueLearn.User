using FluentAssertions;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class ContextKeywordExtractorTests
{
    [Fact]
    public void ExtractTechnologyKeywords_Detects_C_Language_And_Excludes_CSharp_And_CPP()
    {
        var k1 = ContextKeywordExtractor.ExtractTechnologyKeywords("Introduction to C programming");
        k1.Should().Contain("c");

        var k2 = ContextKeywordExtractor.ExtractTechnologyKeywords("C# basics with .NET");
        k2.Should().Contain("c#");
        k2.Should().NotContain("c");

        var k3 = ContextKeywordExtractor.ExtractTechnologyKeywords("C++ OOP and STL");
        k3.Should().Contain("c++");
        k3.Should().NotContain("c");
    }

    [Fact]
    public void ExtractTechnologyKeywords_Detects_Java_But_Not_Javascript()
    {
        var k1 = ContextKeywordExtractor.ExtractTechnologyKeywords("Java Spring and Servlet");
        k1.Should().Contain("java");
        k1.Should().Contain("spring");
        k1.Should().Contain("java-web");

        var k2 = ContextKeywordExtractor.ExtractTechnologyKeywords("JavaScript React and Vue");
        k2.Should().Contain("javascript");
        k2.Should().Contain("react");
        k2.Should().Contain("vue");
        k2.Should().NotContain("java");
    }

    [Fact]
    public void ExtractTechnologyKeywords_Detects_Mobile_And_Database_Terms()
    {
        var k = ContextKeywordExtractor.ExtractTechnologyKeywords("Android Kotlin SQL database");
        k.Should().Contain(new[] { "android", "kotlin", "sql", "database" });
    }
}