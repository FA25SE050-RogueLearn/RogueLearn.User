using System.Linq;
using FluentAssertions;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class SearchQueryBuilderTests
{
    [Fact]
    public void BuildQueryVariants_Programming_WithContext_Includes_Context_Tokens()
    {
        var list = SearchQueryBuilder.BuildQueryVariants("RecyclerView", "Android Kotlin", SubjectCategory.Programming);
        list.Should().NotBeEmpty();
        list.First().ToLowerInvariant().Should().Contain("android");
    }

    [Fact]
    public void BuildQueryVariants_ComputerScience_Vietnamese_Topic_Contains_Vietnamese_Phrase()
    {
        var list = SearchQueryBuilder.BuildQueryVariants("Thu thập yêu cầu", null, SubjectCategory.ComputerScience);
        list.Should().Contain(x => x.Contains("tài liệu học tập"));
    }

    [Fact]
    public void BuildContextAwareQuery_Default_General_Uses_Guide_Tutorial()
    {
        var q = SearchQueryBuilder.BuildContextAwareQuery("Binary trees", null, SubjectCategory.General);
        q.ToLowerInvariant().Should().Contain("guide");
    }
}