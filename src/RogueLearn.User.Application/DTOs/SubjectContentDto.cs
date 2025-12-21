namespace RogueLearn.User.Application.DTOs;

/// <summary>
/// Data Transfer Object for subject content (syllabus JSON).
/// Contains the raw JSONB content stored in the Subject entity.
/// </summary>
public class SubjectContentDto
{
    public Guid SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;   
    public string SubjectName { get; set; } = string.Empty;
    public SyllabusContentDto Content { get; set; } = new();
    public DateTimeOffset? LastUpdated { get; set; }
}

/// <summary>
/// Represents the structure of the syllabus JSON content.
/// This maps directly to the Subject.Content JSONB column.
/// </summary>
public class SyllabusContentDto
{
   
    public string? CourseDescription { get; set; }
    public List<CourseLearningOutcomeDto> CourseLearningOutcomes { get; set; } = new();
    public List<SessionScheduleDto> SessionSchedule { get; set; } = new();
    public List<AssessmentDto> Assessments { get; set; } = new();
    public List<string> RequiredTexts { get; set; } = new();
    public List<string> RecommendedTexts { get; set; } = new();
    public string? GradingPolicy { get; set; }
    public string? AttendancePolicy { get; set; }
    public List<string> Prerequisites { get; set; } = new();
    public List<string> AdditionalResources { get; set; } = new();
}

/// <summary>
/// Course Learning Outcome DTO.
/// </summary>
public class CourseLearningOutcomeDto
{
    public string Id { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? BloomLevel { get; set; } // Remember, Understand, Apply, Analyze, Evaluate, Create
}

/// <summary>
/// Session schedule entry DTO.
/// </summary>
public class SessionScheduleDto
{
    public int SessionNumber { get; set; }
    public string Topic { get; set; } = string.Empty;
    public List<string> Activities { get; set; } = new();
    public List<string> Readings { get; set; } = new();
    public List<ConstructiveQuestionDto> ConstructiveQuestions { get; set; } = new();
    public List<string> MappedSkills { get; set; } = new();
}

/// <summary>
/// Constructive question for active learning.
/// </summary>
public class ConstructiveQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public string? ExpectedAnswer { get; set; }
}

/// <summary>
/// Assessment component DTO.
/// </summary>
public class AssessmentDto
{
    public string Type { get; set; } = string.Empty; // Quiz, Exam, Project, etc.
    public decimal WeightPercentage { get; set; }
    public string? Description { get; set; }
    public int? CountPerSemester { get; set; }
}
