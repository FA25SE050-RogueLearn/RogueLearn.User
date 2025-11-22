namespace RogueLearn.User.Application.DTOs;

/// <summary>
/// Data Transfer Object for subject content (syllabus JSON).
/// Contains the raw JSONB content stored in the Subject entity.
/// </summary>
public class SubjectContentDto
{
    /// <summary>
    /// The subject ID this content belongs to.
    /// </summary>
    public Guid SubjectId { get; set; }

    /// <summary>
    /// Subject code (e.g., "BIT_SE_K19D_K20A").
    /// </summary>
    public string SubjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Subject name.
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// The JSON content object containing all syllabus information.
    /// This is the full SyllabusContent object deserialized from the database JSONB column.
    /// </summary>
    public SyllabusContentDto Content { get; set; } = new();

    /// <summary>
    /// Timestamp of last update.
    /// </summary>
    public DateTimeOffset? LastUpdated { get; set; }
}

/// <summary>
/// Represents the structure of the syllabus JSON content.
/// This maps directly to the Subject.Content JSONB column.
/// </summary>
public class SyllabusContentDto
{
    /// <summary>
    /// Overall course description and objectives.
    /// </summary>
    public string? CourseDescription { get; set; }

    /// <summary>
    /// Course learning outcomes (CLOs).
    /// </summary>
    public List<CourseLearningOutcomeDto> CourseLearningOutcomes { get; set; } = new();

    /// <summary>
    /// Structured session schedule.
    /// </summary>
    public List<SessionScheduleDto> SessionSchedule { get; set; } = new();

    /// <summary>
    /// Assessment methods and weightages.
    /// </summary>
    public List<AssessmentDto> Assessments { get; set; } = new();

    /// <summary>
    /// Required reading materials.
    /// </summary>
    public List<string> RequiredTexts { get; set; } = new();

    /// <summary>
    /// Recommended reading materials.
    /// </summary>
    public List<string> RecommendedTexts { get; set; } = new();

    /// <summary>
    /// Grading policy explanation.
    /// </summary>
    public string? GradingPolicy { get; set; }

    /// <summary>
    /// Attendance and participation policy.
    /// </summary>
    public string? AttendancePolicy { get; set; }

    /// <summary>
    /// Course prerequisites if any.
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>
    /// Additional resources or materials.
    /// </summary>
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
