namespace RogueLearn.User.Application.Features.Quests.Commands.StartQuest;

public record StartQuestResponse
{
    public Guid AttemptId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string AssignedDifficulty { get; init; } = "Standard";
    public bool IsNew { get; init; }
}