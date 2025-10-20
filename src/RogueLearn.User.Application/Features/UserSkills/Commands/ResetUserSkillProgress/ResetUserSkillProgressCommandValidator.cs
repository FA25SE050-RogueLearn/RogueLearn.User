using FluentValidation;

namespace RogueLearn.User.Application.Features.UserSkills.Commands.ResetUserSkillProgress;

public sealed class ResetUserSkillProgressCommandValidator : AbstractValidator<ResetUserSkillProgressCommand>
{
    public ResetUserSkillProgressCommandValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.SkillName)
            .NotEmpty()
            .MaximumLength(255);
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(5);
    }
}