using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestStepContent;

public class QuestStepContentResponse
{
    public List<QuestStepActivity> Activities { get; set; } = new();
}

public class QuestStepActivity
{
    [JsonPropertyName("activityId")]
    public string ActivityId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("skillId")]
    public string? SkillId { get; set; }

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}