// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordResponse.cs
namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid LearningPathId { get; set; }
    public int SubjectsProcessed { get; set; }
    public int QuestsGenerated { get; set; }
    public double CalculatedGpa { get; set; }

    /// <summary>
    /// XP awards from academic grades using tiered cap system.
    /// Only includes NEW awards (re-uploads won't duplicate).
    /// </summary>
    public XpAwardSummary? XpAwarded { get; set; }
}

/// <summary>
/// Summary of XP awarded from academic record processing.
/// </summary>
public class XpAwardSummary
{
    /// <summary>Total XP awarded across all skills.</summary>
    public int TotalXp { get; set; }

    /// <summary>Number of distinct skills that received XP.</summary>
    public int SkillsAffected { get; set; }

    /// <summary>Detailed breakdown per skill.</summary>
    public List<SkillXpAward> SkillAwards { get; set; } = new();
}

/// <summary>
/// XP award for a single skill from academic grades.
/// </summary>
public class SkillXpAward
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;

    /// <summary>XP earned from this transcript upload.</summary>
    public int XpAwarded { get; set; }

    /// <summary>User's new total XP for this skill.</summary>
    public int NewTotalXp { get; set; }

    /// <summary>User's new level for this skill.</summary>
    public int NewLevel { get; set; }

    /// <summary>Subject that contributed this XP.</summary>
    public string SourceSubjectCode { get; set; } = string.Empty;

    /// <summary>Grade achieved (e.g., "8.0").</summary>
    public string Grade { get; set; } = string.Empty;

    /// <summary>Tier description (e.g., "Foundation (Semester 1-3)").</summary>
    public string TierDescription { get; set; } = string.Empty;
}