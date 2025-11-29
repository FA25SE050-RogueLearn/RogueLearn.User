namespace RogueLearn.User.Application.Models;

using System.Text.Json.Serialization;

public class SyllabusSessionDto
{
    public int SessionNumber { get; set; }
    public string Topic { get; set; } = string.Empty;
    public List<string> Activities { get; set; } = new();
    public List<string> Readings { get; set; } = new();

    [JsonPropertyName("suggestedUrl")]
    public string? SuggestedUrl { get; set; }
}
