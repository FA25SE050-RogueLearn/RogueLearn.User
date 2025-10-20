using MediatR;

namespace RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;

public class UpdateAchievementCommand : IRequest<UpdateAchievementResponse>
{
    public Guid Id { get; set; }

    // Unique key, may be updated
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? RuleType { get; set; }
    public string? RuleConfig { get; set; }

    public string? Category { get; set; }
    public string? Icon { get; set; }

    public string? IconUrl { get; set; }

    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public string SourceService { get; set; } = string.Empty;
}