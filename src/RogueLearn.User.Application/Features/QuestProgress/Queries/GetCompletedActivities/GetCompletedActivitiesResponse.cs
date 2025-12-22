namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetCompletedActivities;

public class GetCompletedActivitiesResponse
{
    public Guid StepId { get; set; }
    public List<ActivityProgressDto> Activities { get; set; } = new();
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
}
