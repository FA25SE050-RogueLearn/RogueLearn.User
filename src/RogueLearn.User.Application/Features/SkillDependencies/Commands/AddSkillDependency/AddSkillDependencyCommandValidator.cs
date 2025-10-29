using FluentValidation;
using RogueLearn.User.Domain.Enums;

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

        // RelationshipType is optional; when provided, it must be a defined enum value
        RuleFor(x => x.RelationshipType)
            .Must(rt => rt == null || Enum.IsDefined(typeof(SkillRelationshipType), rt))
            .WithMessage("RelationshipType must be one of: Prerequisite, Corequisite, Recommended.");
    }
}