using RogueLearn.User.Domain.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

public class CurriculumProgramDetailsResponse
{
    public Guid Id { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramCode { get; set; } = string.Empty;
    public string? Description { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public DegreeLevel DegreeLevel { get; set; }

    public int? TotalCredits { get; set; }
    public int? DurationYears { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<CurriculumVersionDetailsDto> CurriculumVersions { get; set; } = new();
    public CurriculumAnalysisDto Analysis { get; set; } = new();
}

public class CurriculumVersionDetailsDto
{
    public Guid Id { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public int EffectiveYear { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<CurriculumSubjectDetailsDto> Subjects { get; set; } = new();
    public CurriculumVersionAnalysisDto Analysis { get; set; } = new();
}

public class CurriculumSubjectDetailsDto
{
    public Guid SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string? Description { get; set; }
    public int? TermNumber { get; set; }
    public bool IsMandatory { get; set; }
    public Guid[]? PrerequisiteSubjectIds { get; set; }

    public SubjectAnalysisDto Analysis { get; set; } = new();
}

public class CurriculumAnalysisDto
{
    public int TotalVersions { get; set; }
    public int TotalSubjects { get; set; }
    public int SubjectsWithContent { get; set; }
    public int SubjectsWithoutContent { get; set; }
    public double ContentCompletionPercentage { get; set; }
    public List<string> MissingContentSubjects { get; set; } = new();
}

public class CurriculumVersionAnalysisDto
{
    public int TotalSubjects { get; set; }
    public int MandatorySubjects { get; set; }
    public int ElectiveSubjects { get; set; }
    public int SubjectsWithContent { get; set; }
    public int SubjectsWithoutContent { get; set; }
    public double ContentCompletionPercentage { get; set; }
    public List<string> MissingContentSubjects { get; set; } = new();
}

public class SubjectAnalysisDto
{
    public bool HasContentInLatestVersion { get; set; }
    public string Status { get; set; } = string.Empty; // "Complete", "Missing"
}