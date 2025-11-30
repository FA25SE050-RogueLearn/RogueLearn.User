// Add to RogueLearn.User.Application/Models/AcademicContext.cs

using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Models;

/// <summary>
/// Enriched academic context for personalized quest generation
/// </summary>
public class AcademicContext
{
    /// <summary>
    /// Current overall GPA
    /// </summary>
    public double CurrentGpa { get; set; }

    /// <summary>
    /// Why is the student taking this quest?
    /// </summary>
    public QuestAttemptReason AttemptReason { get; set; }

    /// <summary>
    /// Performance on prerequisite subjects
    /// </summary>
    public List<PrerequisitePerformance> PrerequisiteHistory { get; set; } = new();

    /// <summary>
    /// Related subjects the student has completed
    /// </summary>
    public List<RelatedSubjectGrade> RelatedSubjects { get; set; } = new();

    /// <summary>
    /// Number of previous attempts (0 = first time)
    /// </summary>
    public int PreviousAttempts { get; set; }

    /// <summary>
    /// Identified strength areas based on past performance
    /// </summary>
    public List<string> StrengthAreas { get; set; } = new();

    /// <summary>
    /// Identified areas needing improvement
    /// </summary>
    public List<string> ImprovementAreas { get; set; } = new();
}

public enum QuestAttemptReason
{
    FirstTime,      // First time taking this subject
    Retake,         // Failed previously, retaking
    CurrentlyStudying, // Currently enrolled in this subject
    Advancement     // Getting ahead in curriculum
}

public class PrerequisitePerformance
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public SubjectEnrollmentStatus Status { get; set; }
    public string PerformanceLevel { get; set; } = string.Empty; // "Strong", "Adequate", "Weak"
}

public class RelatedSubjectGrade
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public double? NumericGrade { get; set; }
}