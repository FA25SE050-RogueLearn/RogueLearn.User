using MediatR;

namespace RogueLearn.User.Application.Features.Achievements.Commands.DeleteAchievement;

public class DeleteAchievementCommand : IRequest
{
    public Guid Id { get; set; }
}