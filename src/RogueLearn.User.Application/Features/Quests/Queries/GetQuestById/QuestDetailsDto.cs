using System.Text.Json;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;

public class QuestDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // Removed IsRecommended, DifficultyLevel, etc.
    public List<QuestStepDto> Steps { get; set; } = new();
}

public class QuestStepDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public int ExperiencePoints { get; set; }

    // Use object type - this will properly serialize the deserialized JSON
    public object? Content { get; set; }
}