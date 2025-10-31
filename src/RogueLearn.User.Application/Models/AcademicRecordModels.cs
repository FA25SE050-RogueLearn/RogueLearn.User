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
}

/// <summary>
/// Represents the response from the learning gap analysis (Transaction 2).
/// </summary>
public class GapAnalysisResponse
{
    [JsonPropertyName("recommendedFocus")]
    public string RecommendedFocus { get; set; } = string.Empty;

    [JsonPropertyName("highestPrioritySubject")]
    public string HighestPrioritySubject { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("forgingPayload")]
    public ForgingPayload ForgingPayload { get; set; } = new();
}

/// <summary>
/// The payload required to initiate the learning path forging process (Transaction 3).
/// </summary>
public class ForgingPayload
{
    [JsonPropertyName("subjectGaps")]
    public List<string> SubjectGaps { get; set; } = new();
}

/// <summary>
/// Represents the successfully created high-level learning path structure (Transaction 3 response).
/// </summary>
public class ForgedLearningPath
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}