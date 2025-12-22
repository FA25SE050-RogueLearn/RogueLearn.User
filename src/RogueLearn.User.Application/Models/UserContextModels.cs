using System.Text.Json;

namespace RogueLearn.User.Application.Models;

public class UserContextDto
{
    public Guid AuthUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public string? PreferencesJson { get; set; }

    public List<string> Roles { get; set; } = new();

    public ClassSummaryDto? Class { get; set; }
    public CurriculumEnrollmentDto? Enrollment { get; set; }

    public SkillSummaryDto Skills { get; set; } = new();
    public int AchievementsCount { get; set; }
}

public class ClassSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RoadmapUrl { get; set; }
    public int DifficultyLevel { get; set; }
    public string[]? SkillFocusAreas { get; set; }
}

public class CurriculumEnrollmentDto
{
    public Guid VersionId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public int EffectiveYear { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly EnrollmentDate { get; set; }
    public DateOnly? ExpectedGraduationDate { get; set; }
}

public class SkillSummaryDto
{
    public int TotalSkills { get; set; }
    public int TotalExperiencePoints { get; set; }
    public int HighestLevel { get; set; }
    public double AverageLevel { get; set; }
    public List<UserSkillDto> TopSkills { get; set; } = new();
}

public class UserSkillDto
{
    public string SkillName { get; set; } = string.Empty;
    public int Level { get; set; }
    public int ExperiencePoints { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
}