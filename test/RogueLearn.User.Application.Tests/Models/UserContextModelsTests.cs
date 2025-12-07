using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class UserContextModelsTests
{
    [Fact]
    public void UserContextDto_Can_Set_Properties()
    {
        var dto = new UserContextDto
        {
            AuthUserId = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            Bio = "Test Bio",
            Class = new ClassSummaryDto { Id = Guid.NewGuid(), Name = "CS", DifficultyLevel = 3, RoadmapUrl = "url", SkillFocusAreas = new[] { "Algorithms" } },
            Enrollment = new CurriculumEnrollmentDto { VersionId = Guid.NewGuid(), VersionCode = "2025A", EffectiveYear = 2025, Status = "Active", EnrollmentDate = new DateOnly(2024, 9, 1), ExpectedGraduationDate = new DateOnly(2028, 6, 1) },
            PreferencesJson = "{}",
            ProfileImageUrl = "http://example.com/profile.jpg",
            Roles = ["Member"],
            Skills = new SkillSummaryDto { TotalSkills = 100, AverageLevel = 3, HighestLevel = 5, TopSkills = [new UserSkillDto { SkillName = "C#", Level = 5 }], TotalExperiencePoints = 1000 },
            AchievementsCount = 100,
        };
        dto.Username.Should().Be("testuser");
        dto.Email.Should().Be("test@example.com");
        dto.DisplayName.Should().Be("Test User");
        dto.Bio.Should().Be("Test Bio");
        dto.Class.Should().NotBeNull();
        dto.Enrollment.Should().NotBeNull();
        dto.PreferencesJson.Should().Be("{}");
        dto.ProfileImageUrl.Should().Be("http://example.com/profile.jpg");
        dto.Roles.Should().Contain("Member");
        dto.Skills.Should().NotBeNull();
        dto.AchievementsCount.Should().Be(100);
        dto.Skills.TotalSkills.Should().Be(100);
        dto.Skills.AverageLevel.Should().Be(3);
        dto.Skills.HighestLevel.Should().Be(5);
        dto.Skills.TopSkills.Should().ContainEquivalentOf(new UserSkillDto { SkillName = "C#", Level = 5 });
        dto.Skills.TotalExperiencePoints.Should().Be(1000);
    }

    [Fact]
    public void CurriculumEnrollmentDto_Can_Set_Properties()
    {
        var dto = new CurriculumEnrollmentDto
        {
            VersionId = Guid.NewGuid(),
            VersionCode = "2025A",
            EffectiveYear = 2025,
            Status = "Active",
            EnrollmentDate = new DateOnly(2024, 9, 1),
            ExpectedGraduationDate = new DateOnly(2028, 6, 1)
        };
        dto.VersionCode.Should().Be("2025A");
        dto.EffectiveYear.Should().Be(2025);
    }

    [Fact]
    public void ClassSummaryDto_Can_Set_Properties()
    {
        var dto = new ClassSummaryDto { Id = Guid.NewGuid(), Name = "CS", DifficultyLevel = 3, RoadmapUrl = "url", SkillFocusAreas = new[] { "Algorithms" } };
        dto.Name.Should().Be("CS");
        dto.SkillFocusAreas.Should().Contain("Algorithms");
    }

    [Fact]
    public void SkillSummaryDto_Can_Set_Properties()
    {
        var dto = new SkillSummaryDto { TotalSkills = 100, AverageLevel = 3, HighestLevel = 5, TopSkills = [], TotalExperiencePoints = 1000 };
        dto.TotalSkills.Should().Be(100);
        dto.AverageLevel.Should().Be(3);
        dto.HighestLevel.Should().Be(5);
        dto.TotalExperiencePoints.Should().Be(1000);
    }

    [Fact]
    public void UserSkillDto_Can_Set_Properties()
    {
        var now = DateTimeOffset.UtcNow;
        var dto = new UserSkillDto { SkillName = "Algo", Level = 3, ExperiencePoints = 200, LastUpdatedAt = now };
        dto.SkillName.Should().Be("Algo");
        dto.Level.Should().Be(3);
        dto.ExperiencePoints.Should().Be(200);
        dto.LastUpdatedAt.Should().Be(now);
    }

    [Fact]
    public void UserSkillDto_Defaults()
    {
        var dto = new UserSkillDto();
        dto.SkillName.Should().Be(string.Empty);
        dto.Level.Should().Be(0);
        dto.ExperiencePoints.Should().Be(0);
        dto.LastUpdatedAt.Should().Be(default);
    }
}