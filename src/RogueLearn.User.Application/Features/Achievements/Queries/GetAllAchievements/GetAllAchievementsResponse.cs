namespace RogueLearn.User.Application.Features.Achievements.Queries.GetAllAchievements;

public class GetAllAchievementsResponse
{
    public List<AchievementDto> Achievements { get; set; } = new();
}