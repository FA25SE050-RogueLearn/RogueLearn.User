using System.ComponentModel.DataAnnotations;
using RogueLearn.User.Domain.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Application.Models;

/// <summary>
/// Root schema for AI-extracted curriculum data
/// </summary>
public class CurriculumImportData
{
    [Required]
    public CurriculumProgramData Program { get; set; } = new();
    
    [Required]
    public CurriculumVersionData Version { get; set; } = new();
    
    [Required]
    public List<SubjectData> Subjects { get; set; } = new();
    
    [Required]
    public List<CurriculumStructureData> Structure { get; set; } = new();
    
    public List<SyllabusData>? Syllabi { get; set; }
}

/// <summary>
/// Program information extracted from curriculum text
/// </summary>
public class CurriculumProgramData
{
    [Required, MaxLength(255)]
    public string ProgramName { get; set; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string ProgramCode { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    [JsonConverter(typeof(StringEnumConverter))]
    public DegreeLevel DegreeLevel { get; set; }
    
    public int? TotalCredits { get; set; }
    
    public double? DurationYears { get; set; }
}

/// <summary>
/// Version information for the curriculum
/// </summary>
public class CurriculumVersionData
{
    [Required, MaxLength(50)]
    public string VersionCode { get; set; } = string.Empty;
    
    [Required]
    public int EffectiveYear { get; set; }
    
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Subject information extracted from curriculum
/// </summary>
public class SubjectData
{
    [Required, MaxLength(50)]
    public string SubjectCode { get; set; } = string.Empty;
    
    [Required, MaxLength(255)]
    public string SubjectName { get; set; } = string.Empty;
    
    [Required, Range(1, 10)]
    public int Credits { get; set; }
    
    public string? Description { get; set; }
}

/// <summary>
/// Curriculum structure mapping subjects to terms
/// </summary>
public class CurriculumStructureData
{
    [Required]
    public string SubjectCode { get; set; } = string.Empty;
    
    [Required, Range(1, 12)]
    public int TermNumber { get; set; }
    
    public bool IsMandatory { get; set; } = true;
    
    public List<string>? PrerequisiteSubjectCodes { get; set; }
    
    public string? PrerequisitesText { get; set; }
}

/// <summary>
/// Syllabus content for subjects
/// </summary>
public class SyllabusData
{
    [Required]
    public string SubjectCode { get; set; } = string.Empty;
    
    [Required]
    public int VersionNumber { get; set; } = 1;
    
    [Required]
    public SyllabusContent Content { get; set; } = new();
    
    public DateOnly? EffectiveDate { get; set; }
    
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Structured syllabus content
/// </summary>
public class SyllabusContent
{
    public string? CourseDescription { get; set; }
    public List<string>? LearningOutcomes { get; set; }
    public List<SyllabusWeek>? WeeklySchedule { get; set; }
    public List<AssessmentItem>? Assessments { get; set; }
    public List<string>? RequiredTexts { get; set; }
    public List<string>? RecommendedTexts { get; set; }
    public string? GradingPolicy { get; set; }
    public string? AttendancePolicy { get; set; }
}

public class SyllabusWeek
{
    public int WeekNumber { get; set; }
    public string? Topic { get; set; }
    public List<string>? Activities { get; set; }
    public List<string>? Readings { get; set; }
}

public class AssessmentItem
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public int? WeightPercentage { get; set; }
    public string? Description { get; set; }
}