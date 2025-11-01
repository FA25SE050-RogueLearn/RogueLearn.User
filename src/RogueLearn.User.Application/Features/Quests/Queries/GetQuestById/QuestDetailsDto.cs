// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetQuestById/QuestDetailsDto.cs
using System.Text.Json; // ADDED: Import for JsonDocument

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
    // MODIFICATION: Added the Content property to carry the step's detailed payload.
    public JsonDocument? Content { get; set; }
}