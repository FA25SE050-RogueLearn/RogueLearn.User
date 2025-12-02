using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class UserContextUserSkillDtoTests
{
    [Fact]
    public void UserSkillDto_Can_Set_Properties()
    {
        var dto = new UserSkillDto { SkillName = "Algo", Level = 3, ExperiencePoints = 200, LastUpdatedAt = DateTimeOffset.UtcNow };
        dto.SkillName.Should().Be("Algo");
        dto.Level.Should().Be(3);
        dto.ExperiencePoints.Should().Be(200);
    }
}