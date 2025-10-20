using FluentValidation;

namespace RogueLearn.User.Application.Features.Skills.Commands.UpdateSkill;

public sealed class UpdateSkillCommandValidator : AbstractValidator<UpdateSkillCommand>
{
    public UpdateSkillCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Tier)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.Domain)
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Domain));

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}