using FluentValidation;

namespace RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;

/// <summary>
/// FluentValidation rules for <see cref="DeleteSubjectCommand"/>.
/// </summary>
public class DeleteSubjectCommandValidator : AbstractValidator<DeleteSubjectCommand>
{
    public DeleteSubjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}