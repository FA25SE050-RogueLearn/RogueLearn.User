using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.SkillDependencies.Queries.GetSkillDependencies;

public class GetSkillDependenciesQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Mapped_Dependencies()
    {
        var repo = Substitute.For<ISkillDependencyRepository>();
        var handler = new GetSkillDependenciesQueryHandler(repo);

        var skillId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var deps = new List<SkillDependency>
        {
            new SkillDependency { Id = Guid.NewGuid(), SkillId = skillId, PrerequisiteSkillId = Guid.NewGuid(), RelationshipType = SkillRelationshipType.Prerequisite, CreatedAt = now },
            new SkillDependency { Id = Guid.NewGuid(), SkillId = skillId, PrerequisiteSkillId = Guid.NewGuid(), RelationshipType = SkillRelationshipType.Corequisite, CreatedAt = now }
        };

        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SkillDependency, bool>>>(), Arg.Any<CancellationToken>()).Returns(deps);

        var query = new GetSkillDependenciesQuery { SkillId = skillId };
        var result = await handler.Handle(query, CancellationToken.None);

        result.Dependencies.Should().HaveCount(2);
        result.Dependencies[0].SkillId.Should().Be(skillId);
        result.Dependencies.Select(d => d.RelationshipType).Should().Contain(new[] { SkillRelationshipType.Prerequisite, SkillRelationshipType.Corequisite });
    }
}