using FluentValidation;

namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.RemoveSkillDependency;

/// <summary>
/// Validator for RemoveSkillDependencyCommand.
/// Ensures identifiers are set and not self-referencing.
/// </summary>
public sealed class RemoveSkillDependencyCommandValidator : AbstractValidator<RemoveSkillDependencyCommand>
{
    public RemoveSkillDependencyCommandValidator()
    {
        RuleFor(x => x.SkillId)
            .NotEmpty();

        RuleFor(x => x.PrerequisiteSkillId)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.SkillId != x.PrerequisiteSkillId)
            .WithMessage("A skill cannot depend on itself.");
    }
}