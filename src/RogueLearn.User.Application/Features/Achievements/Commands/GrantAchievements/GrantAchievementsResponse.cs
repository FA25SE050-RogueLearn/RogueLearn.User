namespace RogueLearn.User.Application.Features.Achievements.Commands.GrantAchievements;

public class GrantAchievementsResponse
{
    public int GrantedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}