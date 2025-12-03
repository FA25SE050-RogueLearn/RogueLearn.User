using FluentAssertions;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;

namespace RogueLearn.User.Application.Tests.Features.Skills.Queries.GetSkillDetail;

public class DependencyStatusDtoTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var dto = new DependencyStatusDto();
        dto.SkillId.Should().Be(Guid.Empty);
        dto.Name.Should().Be(string.Empty);
        dto.IsMet.Should().BeFalse();
        dto.UserLevel.Should().Be(0);
        dto.StatusLabel.Should().Be(string.Empty);
    }

    [Fact]
    public void Properties_ShouldBeAssignable()
    {
        var id = Guid.NewGuid();
        var dto = new DependencyStatusDto
        {
            SkillId = id,
            Name = "Functions",
            IsMet = true,
            UserLevel = 3,
            StatusLabel = "100% Complete"
        };

        dto.SkillId.Should().Be(id);
        dto.Name.Should().Be("Functions");
        dto.IsMet.Should().BeTrue();
        dto.UserLevel.Should().Be(3);
        dto.StatusLabel.Should().Be("100% Complete");
    }
}