// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestFeedback/Commands/ResolveQuestStepFeedback/ResolveQuestStepFeedbackCommand.cs
using MediatR;

namespace RogueLearn.User.Application.Features.QuestFeedback.Commands.ResolveQuestStepFeedback;

public class ResolveQuestStepFeedbackCommand : IRequest
{
    public Guid FeedbackId { get; set; }
    public bool IsResolved { get; set; }
    public string? AdminNotes { get; set; }
}