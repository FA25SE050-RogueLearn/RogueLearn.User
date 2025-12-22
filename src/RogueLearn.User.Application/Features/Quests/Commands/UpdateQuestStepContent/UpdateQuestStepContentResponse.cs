namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepContent;

public class UpdateQuestStepContentResponse
{
    public Guid QuestStepId { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}