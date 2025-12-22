using FluentValidation;

namespace RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;

/// <summary>
/// FluentValidation rules for <see cref="UpdateSubjectCommand"/>.
/// </summary>
public class UpdateSubjectCommandValidator : AbstractValidator<UpdateSubjectCommand>
{
    public UpdateSubjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.SubjectCode)
            .NotEmpty().WithMessage("SubjectCode is required.")
            .MaximumLength(50).WithMessage("SubjectCode must be 50 characters or fewer.");

        RuleFor(x => x.SubjectName)
            .NotEmpty().WithMessage("SubjectName is required.")
            .MaximumLength(200).WithMessage("SubjectName must be 200 characters or fewer.");

        RuleFor(x => x.Credits)
            .GreaterThan(0).WithMessage("Credits must be greater than 0.")
            .LessThanOrEqualTo(50).WithMessage("Credits must be 50 or fewer.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must be 1000 characters or fewer.");
    }
}