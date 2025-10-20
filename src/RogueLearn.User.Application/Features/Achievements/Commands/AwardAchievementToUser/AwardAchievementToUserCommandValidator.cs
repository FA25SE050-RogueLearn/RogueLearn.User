using FluentValidation;

namespace RogueLearn.User.Application.Features.Achievements.Commands.AwardAchievementToUser;

public class AwardAchievementToUserCommandValidator : AbstractValidator<AwardAchievementToUserCommand>
{
    public AwardAchievementToUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AchievementId).NotEmpty();
        RuleFor(x => x.Context)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Context));
    }
}