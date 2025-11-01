// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Commands/DeleteLearningPath/DeleteLearningPathCommandValidator.cs
using FluentValidation;

namespace RogueLearn.User.Application.Features.LearningPaths.Commands.DeleteLearningPath;

/// <summary>
/// FluentValidation rules for <see cref="DeleteLearningPathCommand"/>.
/// </summary>
public class DeleteLearningPathCommandValidator : AbstractValidator<DeleteLearningPathCommand>
{
    public DeleteLearningPathCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}