using MediatR;

namespace RogueLearn.User.Application.Features.Achievements.Commands.GrantAchievements;

public class GrantAchievementsCommand : IRequest<GrantAchievementsResponse>
{
    public List<GrantAchievementEntry> Entries { get; set; } = new();
}

public class GrantAchievementEntry
{
    public string UserId { get; set; } = string.Empty;
    public string AchievementKey { get; set; } = string.Empty;
}