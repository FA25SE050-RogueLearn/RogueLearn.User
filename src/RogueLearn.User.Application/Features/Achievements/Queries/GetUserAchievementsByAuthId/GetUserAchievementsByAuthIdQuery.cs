using MediatR;

namespace RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;

public class GetUserAchievementsByAuthIdQuery : IRequest<GetUserAchievementsByAuthIdResponse>
{
    public Guid AuthUserId { get; set; }
}