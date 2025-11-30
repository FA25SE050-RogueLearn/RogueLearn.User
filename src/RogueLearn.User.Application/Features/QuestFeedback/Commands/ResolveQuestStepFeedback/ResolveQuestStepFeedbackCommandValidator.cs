// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestFeedback/Commands/ResolveQuestStepFeedback/ResolveQuestStepFeedbackCommandValidator.cs
using FluentValidation;

namespace RogueLearn.User.Application.Features.QuestFeedback.Commands.ResolveQuestStepFeedback;

public class ResolveQuestStepFeedbackCommandValidator : AbstractValidator<ResolveQuestStepFeedbackCommand>
{
    public ResolveQuestStepFeedbackCommandValidator()
    {
        RuleFor(x => x.FeedbackId).NotEmpty();
        RuleFor(x => x.AdminNotes).MaximumLength(1000);
    }
}