using FluentValidation;

namespace RogueLearn.User.Application.Features.Achievements.Commands.RevokeAchievementFromUser;

public class RevokeAchievementFromUserCommandValidator : AbstractValidator<RevokeAchievementFromUserCommand>
{
    public RevokeAchievementFromUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AchievementId).NotEmpty();
    }
}