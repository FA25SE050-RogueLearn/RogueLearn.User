using FluentAssertions;
using RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;

namespace RogueLearn.User.Application.Tests.Features.SkillDependencies.Queries.GetSkillDependencies;

public class GetSkillDependenciesResponseTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var resp = new GetSkillDependenciesResponse();
        resp.Dependencies.Should().NotBeNull();
        resp.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeAssignable()
    {
        var resp = new GetSkillDependenciesResponse
        {
            Dependencies = [ new SkillDependencyDto { Id = Guid.NewGuid() } ]
        };

        resp.Dependencies.Should().HaveCount(1);
    }
}