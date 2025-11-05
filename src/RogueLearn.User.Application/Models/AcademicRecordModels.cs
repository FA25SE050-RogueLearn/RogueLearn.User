// RogueLearn.User/src/RogueLearn.User.Application/Models/AcademicRecordModels.cs
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Models;

/// <summary>
/// Represents the structured data extracted from a user's FAP HTML.
/// This is the primary output of Transaction 1.
/// </summary>
public class FapRecordData
{
    [JsonPropertyName("gpa")]
    public double? Gpa { get; set; }

    [JsonPropertyName("totalCredits")]
    public int? TotalCredits { get; set; } // Optional: total credits calculated

    [JsonPropertyName("subjects")]
    public List<FapSubjectData> Subjects { get; set; } = new();
}

public class FapSubjectData
{
    [JsonPropertyName("subjectCode")]
    public string SubjectCode { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("mark")]
    public double? Mark { get; set; }

    [JsonPropertyName("credits")]
    public int? Credits { get; set; } // Optional: credits for this subject
}
