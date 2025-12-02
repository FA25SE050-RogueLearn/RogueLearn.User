using FluentAssertions;
using RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Tests.Features.SkillDependencies.Queries.GetSkillDependencies;

public class SkillDependencyDtoTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var dto = new SkillDependencyDto();
        dto.Id.Should().Be(Guid.Empty);
        dto.SkillId.Should().Be(Guid.Empty);
        dto.PrerequisiteSkillId.Should().Be(Guid.Empty);
        dto.RelationshipType.Should().Be(SkillRelationshipType.Prerequisite);
        dto.CreatedAt.Should().Be(default);
    }

    [Fact]
    public void Properties_ShouldBeAssignable()
    {
        var dto = new SkillDependencyDto
        {
            Id = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            PrerequisiteSkillId = Guid.NewGuid(),
            RelationshipType = SkillRelationshipType.Corequisite,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dto.RelationshipType.Should().Be(SkillRelationshipType.Corequisite);
        dto.SkillId.Should().NotBe(Guid.Empty);
    }
}