using FluentValidation;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Commands.AddSubjectToCurriculum;

/// <summary>
/// Validator for AddSubjectToCurriculumCommand.
/// Ensures required identifiers and sensible field constraints.
/// </summary>
public sealed class AddSubjectToCurriculumCommandValidator : AbstractValidator<AddSubjectToCurriculumCommand>
{
    public AddSubjectToCurriculumCommandValidator()
    {
        RuleFor(x => x.CurriculumVersionId)
            .NotEmpty();

        RuleFor(x => x.SubjectId)
            .NotEmpty();

        RuleFor(x => x.TermNumber)
            .GreaterThan(0);

        When(x => x.PrerequisiteSubjectIds != null, () =>
        {
            RuleForEach(x => x.PrerequisiteSubjectIds!)
                .NotEmpty();

            RuleFor(x => x.PrerequisiteSubjectIds!)
                .Must(ids => ids.Distinct().Count() == ids.Length)
                .WithMessage("PrerequisiteSubjectIds must be unique.");
        });

        RuleFor(x => x.PrerequisitesText)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.PrerequisitesText));
    }
}