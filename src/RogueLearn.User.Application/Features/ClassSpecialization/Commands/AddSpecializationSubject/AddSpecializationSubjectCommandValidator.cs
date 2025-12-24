using FluentValidation;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;

/// <summary>
/// FluentValidation rules for <see cref="AddSpecializationSubjectCommand"/>.
/// Checklist compliance:
/// - Validate required inputs (ClassId, SubjectId).
/// - Rely on pipeline behavior to stop early on validation failures.
/// </summary>
public class AddSpecializationSubjectCommandValidator : AbstractValidator<AddSpecializationSubjectCommand>
{
    public AddSpecializationSubjectCommandValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty().WithMessage("ClassId is required.");

        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("SubjectId is required.");
    }
}