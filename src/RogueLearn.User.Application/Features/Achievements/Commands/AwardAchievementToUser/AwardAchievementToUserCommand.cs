using MediatR;

namespace RogueLearn.User.Application.Features.Achievements.Commands.AwardAchievementToUser;

public class AwardAchievementToUserCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid AchievementId { get; set; }
    public string? Context { get; set; }
}