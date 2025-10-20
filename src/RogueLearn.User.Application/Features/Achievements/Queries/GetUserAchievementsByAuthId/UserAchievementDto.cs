namespace RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;

public class UserAchievementDto
{
    public Guid AchievementId { get; set; }

    // Include achievement key for stable references
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string SourceService { get; set; } = string.Empty;
    public DateTimeOffset EarnedAt { get; set; }
    public string? Context { get; set; }
}