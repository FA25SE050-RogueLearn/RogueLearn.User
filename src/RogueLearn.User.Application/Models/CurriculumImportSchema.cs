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

    // Basic Information
    public int SyllabusId { get; set; }
    public string SyllabusName { get; set; } = string.Empty;
    public string SyllabusEnglish { get; set; } = string.Empty;
    public int NoCredit { get; set; }
    public string DegreeLevel { get; set; } = string.Empty;
    public string TimeAllocation { get; set; } = string.Empty;
    public string PreRequisite { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> StudentTasks { get; set; } = new();
    public List<string> Tools { get; set; } = new();
    
    // Approval Information
    public string DecisionNo { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateOnly? ApprovedDate { get; set; }
    
    // Materials
    public List<SyllabusMaterial> Materials { get; set; } = new();
    
    // Learning Outcomes
    public List<LearningOutcome> LearningOutcomes { get; set; } = new();
    
    // Sessions
    public List<SyllabusSession> Sessions { get; set; } = new();
    
    // Questions and Assessments
    public List<ConstructiveQuestion> ConstructiveQuestions { get; set; } = new();
    public List<SyllabusAssessment> Assessments { get; set; } = new();
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

public class SyllabusMaterial
{
    public string MaterialDescription { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public DateOnly? PublishedDate { get; set; }
    public string Edition { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public bool IsMainMaterial { get; set; }
    public bool IsHardCopy { get; set; }
    public bool IsOnline { get; set; }
    public string Note { get; set; } = string.Empty;
}

public class LearningOutcome
{
    public int CLONumber { get; set; }
    public string CLOName { get; set; } = string.Empty;
    public string CLODetails { get; set; } = string.Empty;
    public string LODetails { get; set; } = string.Empty;
}

public class SyllabusSession
{
    public int SessionNumber { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string LearningTeachingType { get; set; } = string.Empty;
    public string LO { get; set; } = string.Empty;
    public string ITU { get; set; } = string.Empty;
    public string StudentMaterials { get; set; } = string.Empty;
    public string SDownload { get; set; } = string.Empty;
    public string StudentTasks { get; set; } = string.Empty;
    public string URLs { get; set; } = string.Empty;
}

public class ConstructiveQuestion
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class SyllabusAssessment
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int WeightPercentage { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Instructions { get; set; } = string.Empty;
}