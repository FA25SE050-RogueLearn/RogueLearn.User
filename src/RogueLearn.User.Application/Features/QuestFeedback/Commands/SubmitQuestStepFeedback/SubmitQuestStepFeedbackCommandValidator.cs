// src/RogueLearn.User.Application/Features/QuestFeedback/Commands/SubmitQuestStepFeedback/SubmitQuestStepFeedbackCommandValidator.cs
using FluentValidation;

namespace RogueLearn.User.Application.Features.QuestFeedback.Commands.SubmitQuestStepFeedback;

public class SubmitQuestStepFeedbackCommandValidator : AbstractValidator<SubmitQuestStepFeedbackCommand>
{
    public SubmitQuestStepFeedbackCommandValidator()
    {
        RuleFor(x => x.AuthUserId).NotEmpty();
        RuleFor(x => x.QuestId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(50)
            .Must(c => new[] { "ContentError", "TechnicalIssue", "TooDifficult", "TooEasy", "Other" }.Contains(c))
            .WithMessage("Category must be one of: ContentError, TechnicalIssue, TooDifficult, TooEasy, Other");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Comment));
    }
}