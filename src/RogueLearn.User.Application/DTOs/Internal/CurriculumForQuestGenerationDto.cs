using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.DTOs.Internal;

public class CurriculumForQuestGenerationDto
{
    [JsonPropertyName("curriculumCode")]
    public string CurriculumCode { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("subjects")]
    public List<SubjectForQuestGenerationDto> Subjects { get; set; } = [];
}

public class SubjectForQuestGenerationDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("semester")]
    public int Semester { get; set; }

    [JsonPropertyName("credits")]
    public int Credits { get; set; }

    [JsonPropertyName("prerequisites")]
    public string? Prerequisites { get; set; }
}