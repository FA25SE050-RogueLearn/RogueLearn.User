namespace RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;

public class UpdateAchievementResponse
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? RuleType { get; set; }
    public string? RuleConfig { get; set; }

    public string? Category { get; set; }
    public string? IconUrl { get; set; }

    public int Version { get; set; }
    public bool IsActive { get; set; }

    public string SourceService { get; set; } = string.Empty;
}