using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class UserContextModelsTests
{
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
}