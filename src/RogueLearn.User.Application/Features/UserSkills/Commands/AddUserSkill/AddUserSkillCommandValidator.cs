using FluentValidation;

namespace RogueLearn.User.Application.Features.UserSkills.Commands.AddUserSkill;

public sealed class AddUserSkillCommandValidator : AbstractValidator<AddUserSkillCommand>
{
    public AddUserSkillCommandValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.SkillName)
            .NotEmpty()
            .MaximumLength(255);
    }
}