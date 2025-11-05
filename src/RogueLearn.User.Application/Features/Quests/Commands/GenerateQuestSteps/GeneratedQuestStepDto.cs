// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestSteps/GeneratedQuestStepDto.cs
using RogueLearn.User.Domain.Enums;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

public class GeneratedQuestStepDto
{
    public Guid Id { get; set; }
    public Guid QuestId { get; set; }
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public StepType StepType { get; set; }
    public object? Content { get; set; }
}