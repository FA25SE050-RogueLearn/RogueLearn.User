using MediatR;

namespace RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;

public class CreateAchievementCommand : IRequest<CreateAchievementResponse>
{
    // Unique key for idempotent upserts and stable references
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Rule definition (optional)
    public string? RuleType { get; set; }
    public string? RuleConfig { get; set; }

    // Categorization and icon metadata (optional)
    public string? Category { get; set; }
    public string? Icon { get; set; }

    public string? IconUrl { get; set; }

    // Versioning & activation
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public string SourceService { get; set; } = string.Empty;
}