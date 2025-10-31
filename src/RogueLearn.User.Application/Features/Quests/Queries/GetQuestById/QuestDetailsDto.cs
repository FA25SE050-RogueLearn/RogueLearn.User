// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetQuestById/QuestDetailsDto.cs
namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;

public class QuestDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<QuestStepDto> Steps { get; set; } = new();
}

public class QuestStepDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
}