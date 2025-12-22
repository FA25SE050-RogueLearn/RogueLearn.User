using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Models;

public class FapRecordData
{
    [JsonPropertyName("gpa")]
    public double? Gpa { get; set; }

    [JsonPropertyName("totalCredits")]
    public int? TotalCredits { get; set; }

    [JsonPropertyName("subjects")]
    public List<FapSubjectData> Subjects { get; set; } = new();
}

public class FapSubjectData
{
    [JsonPropertyName("subjectCode")]
    public string SubjectCode { get; set; } = string.Empty;

    [JsonPropertyName("subjectName")]
    public string? SubjectName { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("mark")]
    public double? Mark { get; set; }

    [JsonPropertyName("credits")]
    public int? Credits { get; set; }

    [JsonPropertyName("semester")]
    public int Semester { get; set; }

    [JsonPropertyName("academicYear")]
    public string AcademicYear { get; set; } = string.Empty;
}