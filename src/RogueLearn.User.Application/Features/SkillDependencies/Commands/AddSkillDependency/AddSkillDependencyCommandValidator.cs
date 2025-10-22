using FluentValidation;

namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;

/// <summary>
/// Validator for AddSkillDependencyCommand.
/// Ensures skill identifiers are set and not self-referencing; relationship type constraints when provided.
/// </summary>
public sealed class AddSkillDependencyCommandValidator : AbstractValidator<AddSkillDependencyCommand>
{
    public AddSkillDependencyCommandValidator()
    {
        RuleFor(x => x.SkillId)
            .NotEmpty();

        RuleFor(x => x.PrerequisiteSkillId)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.SkillId != x.PrerequisiteSkillId)
            .WithMessage("A skill cannot depend on itself.");

        When(x => !string.IsNullOrWhiteSpace(x.RelationshipType), () =>
        {
            RuleFor(x => x.RelationshipType!)
                .MaximumLength(100)
                .Matches("^[a-zA-Z0-9 _-]+$")
                .WithMessage("RelationshipType may only contain letters, numbers, spaces, underscores, and hyphens.");
        });
    }
}