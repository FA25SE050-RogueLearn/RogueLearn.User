namespace RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;

public class GetUserAchievementsByAuthIdResponse
{
    public List<UserAchievementDto> Achievements { get; set; } = new();
}