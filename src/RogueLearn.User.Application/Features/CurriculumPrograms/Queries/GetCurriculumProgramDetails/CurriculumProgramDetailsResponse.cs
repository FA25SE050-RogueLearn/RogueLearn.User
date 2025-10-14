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
    public int TermNumber { get; set; }
    public bool IsMandatory { get; set; }
    public Guid[]? PrerequisiteSubjectIds { get; set; }
    public string? PrerequisitesText { get; set; }
    
    public List<SyllabusVersionDetailsDto> SyllabusVersions { get; set; } = new();
    public SubjectAnalysisDto Analysis { get; set; } = new();
}

public class SyllabusVersionDetailsDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    public bool HasContent { get; set; }
}

public class CurriculumAnalysisDto
{
    public int TotalVersions { get; set; }
    public int ActiveVersions { get; set; }
    public int TotalSubjects { get; set; }
    public int SubjectsWithSyllabus { get; set; }
    public int SubjectsWithoutSyllabus { get; set; }
    public int TotalSyllabusVersions { get; set; }
    public double SyllabusCompletionPercentage { get; set; }
    public List<string> MissingContentSubjects { get; set; } = new();
}

public class CurriculumVersionAnalysisDto
{
    public int TotalSubjects { get; set; }
    public int MandatorySubjects { get; set; }
    public int ElectiveSubjects { get; set; }
    public int SubjectsWithSyllabus { get; set; }
    public int SubjectsWithoutSyllabus { get; set; }
    public int TotalSyllabusVersions { get; set; }
    public double SyllabusCompletionPercentage { get; set; }
    public List<string> MissingContentSubjects { get; set; } = new();
}

public class SubjectAnalysisDto
{
    public int TotalSyllabusVersions { get; set; }
    public int ActiveSyllabusVersions { get; set; }
    public bool HasAnySyllabus { get; set; }
    public bool HasActiveSyllabus { get; set; }
    public bool HasContentInLatestVersion { get; set; }
    public string Status { get; set; } = string.Empty; // "Complete", "Missing", "Incomplete"
}