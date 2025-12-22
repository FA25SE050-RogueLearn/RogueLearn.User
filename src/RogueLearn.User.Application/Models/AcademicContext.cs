using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Models;

public class AcademicContext
{
    public double CurrentGpa { get; set; }
    public QuestAttemptReason AttemptReason { get; set; }
    public List<PrerequisitePerformance> PrerequisiteHistory { get; set; } = new();
    public List<RelatedSubjectGrade> RelatedSubjects { get; set; } = new();
    public int PreviousAttempts { get; set; }
    public List<string> StrengthAreas { get; set; } = new();
    public List<string> ImprovementAreas { get; set; } = new();
}

public enum QuestAttemptReason
{
    FirstTime,     
    Retake,     
    CurrentlyStudying, 
    Advancement    
}

public class PrerequisitePerformance
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public SubjectEnrollmentStatus Status { get; set; }
    public string PerformanceLevel { get; set; } = string.Empty;
}

public class RelatedSubjectGrade
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public double? NumericGrade { get; set; }
}