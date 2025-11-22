using MediatR;

namespace RogueLearn.User.Application.Features.Achievements.Commands.GrantAchievements;

public class GrantAchievementsCommand : IRequest<GrantAchievementsResponse>
{
    public List<GrantAchievementEntry> Entries { get; set; } = new();
}

public class GrantAchievementEntry
{
    public Guid AuthUserId { get; set; }
    public string AchievementKey { get; set; } = string.Empty;
}