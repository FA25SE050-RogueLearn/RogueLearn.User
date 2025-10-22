// RogueLearn.User/src/RogueLearn.User.Application/Features/ClassSpecialization/Commands/AddSpecializationSubject/AddSpecializationSubjectCommandValidator.cs
using FluentValidation;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;

/// <summary>
/// FluentValidation rules for <see cref="AddSpecializationSubjectCommand"/>.
/// Checklist compliance:
/// - Validate required inputs (ClassId, SubjectId).
/// - Sanity check for Semester and placeholder code length.
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

        RuleFor(x => x.Semester)
            .GreaterThan(0).WithMessage("Semester must be greater than 0.");

        RuleFor(x => x.PlaceholderSubjectCode)
            .NotNull()
            .MaximumLength(50).WithMessage("PlaceholderSubjectCode must be 50 characters or fewer.");
    }
}