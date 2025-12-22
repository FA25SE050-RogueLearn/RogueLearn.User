using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestProgress;

public class QuestStepProgressDto
{
    public Guid StepId { get; set; }
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DifficultyVariant { get; set; } = string.Empty;
    public StepCompletionStatus Status { get; set; }
    public bool IsLocked { get; set; }
    public int CompletedActivitiesCount { get; set; }
    public int TotalActivitiesCount { get; set; }
}