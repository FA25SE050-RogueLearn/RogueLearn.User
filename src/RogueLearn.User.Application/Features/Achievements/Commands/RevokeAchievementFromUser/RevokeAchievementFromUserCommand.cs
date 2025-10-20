using MediatR;

namespace RogueLearn.User.Application.Features.Achievements.Commands.RevokeAchievementFromUser;

public class RevokeAchievementFromUserCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid AchievementId { get; set; }
}