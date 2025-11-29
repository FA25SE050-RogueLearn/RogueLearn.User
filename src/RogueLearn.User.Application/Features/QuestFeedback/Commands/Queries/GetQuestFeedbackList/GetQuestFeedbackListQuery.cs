// src/RogueLearn.User.Application/Features/QuestFeedback/Queries/GetQuestFeedbackList/GetQuestFeedbackListQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.QuestFeedback.Queries.GetQuestFeedbackList;

public class GetQuestFeedbackListQuery : IRequest<List<QuestFeedbackDto>>
{
    public bool UnresolvedOnly { get; set; } = true;
    public Guid? QuestId { get; set; }
}

public class QuestFeedbackDto
{
    public Guid Id { get; set; }
    public Guid QuestId { get; set; }
    public Guid StepId { get; set; }
    public Guid AuthUserId { get; set; }
    public int Rating { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public bool IsResolved { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}