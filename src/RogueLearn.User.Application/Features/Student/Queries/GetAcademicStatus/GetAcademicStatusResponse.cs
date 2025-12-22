namespace RogueLearn.User.Application.Features.Student.Queries.GetAcademicStatus;

public class GetAcademicStatusResponse
{
    public Guid? EnrollmentId { get; set; }
    public Guid? CurriculumVersionId { get; set; }
    public string CurriculumProgramName { get; set; } = string.Empty;
    public double CurrentGpa { get; set; }
    public int TotalSubjects { get; set; }
    public int CompletedSubjects { get; set; }
    public int InProgressSubjects { get; set; }
    public int FailedSubjects { get; set; }
    public Guid? LearningPathId { get; set; }
    public int TotalQuests { get; set; }
    public int CompletedQuests { get; set; }
    public SkillInitializationInfo SkillInitialization { get; set; } = new();
    public List<SubjectProgressDto> Subjects { get; set; } = new();
    public List<ChapterProgressDto> Chapters { get; set; } = new();
}

public class SkillInitializationInfo
{
    public bool IsInitialized { get; set; }
    public int TotalSkills { get; set; }
    public DateTimeOffset? LastInitializedAt { get; set; }
}

public class SubjectProgressDto
{
    public Guid SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Semester { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public Guid? QuestId { get; set; }
    public string? QuestStatus { get; set; }
}

public class ChapterProgressDto
{
    public Guid ChapterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalQuests { get; set; }
    public int CompletedQuests { get; set; }
}