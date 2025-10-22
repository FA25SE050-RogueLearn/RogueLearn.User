using FluentValidation;

namespace RogueLearn.User.Application.Features.Skills.Commands.DeleteSkill;

/// <summary>
/// Validator for DeleteSkillCommand.
/// Ensures required fields are present.
/// </summary>
public sealed class DeleteSkillCommandValidator : AbstractValidator<DeleteSkillCommand>
{
    public DeleteSkillCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}