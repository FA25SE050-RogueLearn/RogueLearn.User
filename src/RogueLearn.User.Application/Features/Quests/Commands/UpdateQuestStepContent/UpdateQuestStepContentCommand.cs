// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestStepContent/UpdateQuestStepContentCommand.cs
using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepContent;

public class UpdateQuestStepContentCommand : IRequest<UpdateQuestStepContentResponse>
{
    [JsonIgnore]
    public Guid QuestStepId { get; set; }

    public List<UpdateQuestStepActivityDto> Activities { get; set; } = new();
}

public class UpdateQuestStepActivityDto
{
    public string ActivityId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? SkillId { get; set; }

    // Using Dictionary<string, object> allows flexible payload validation
    public Dictionary<string, object>? Payload { get; set; }
}