using FluentAssertions;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class SearchResultParserTests
{
    [Fact]
    public void ExtractUrlFromSearchResult_Parses_Link_Line()
    {
        var input = "Title: Example\nSnippet: Something\nLink: https://example.com/path\n";
        var url = SearchResultParser.ExtractUrlFromSearchResult(input);
        url.Should().Be("https://example.com/path");
    }
}