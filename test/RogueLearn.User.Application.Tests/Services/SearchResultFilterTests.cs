using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class SearchResultFilterTests
{
    [Fact]
    public void IsWrongFramework_Blocks_Python_In_C_Context()
    {
        var url = "https://docs.python.org/3/tutorial/";
        var tech = new List<string> { "c" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "pointers", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }

    [Fact]
    public void IsWrongFramework_Blocks_Oracle_In_DotNet_Context()
    {
        var url = "https://docs.oracle.com/javase/tutorial/";
        var tech = new List<string> { "c#", ".net" };
        var isWrong = SearchResultFilter.IsWrongFramework(url, tech, SubjectCategory.Programming, "ASP.NET MVC", Substitute.For<ILogger>());
        isWrong.Should().BeTrue();
    }
}