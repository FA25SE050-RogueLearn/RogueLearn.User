using FluentValidation;

namespace RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;

public sealed class CreateSkillCommandValidator : AbstractValidator<CreateSkillCommand>
{
    public CreateSkillCommandValidator()
    {
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