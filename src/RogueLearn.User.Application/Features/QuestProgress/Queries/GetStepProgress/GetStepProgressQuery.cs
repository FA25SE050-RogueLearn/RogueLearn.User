// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestProgress/Queries/GetStepProgress/GetStepProgressQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetStepProgress;

public class GetStepProgressQuery : IRequest<GetStepProgressResponse>
{
    public Guid AuthUserId { get; set; }
    public Guid QuestId { get; set; }
    public Guid StepId { get; set; }
}

public class GetStepProgressResponse
{
    public Guid StepId { get; set; }
    public string? StepTitle { get; set; }
    public string Status { get; set; } // InProgress, Completed
    public int CompletedActivitiesCount { get; set; }
    public int TotalActivitiesCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid[] CompletedActivityIds { get; set; } = Array.Empty<Guid>();
    public decimal ProgressPercentage { get; set; }
}