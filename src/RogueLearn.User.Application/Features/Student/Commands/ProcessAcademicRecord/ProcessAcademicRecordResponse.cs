// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/ProcessAcademicRecord/ProcessAcademicRecordResponse.cs
using RogueLearn.User.Application.Models; // For AcademicAnalysisReport

namespace RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid LearningPathId { get; set; }
    public int SubjectsProcessed { get; set; }
    public int QuestsGenerated { get; set; }
    public double CalculatedGpa { get; set; }

    public XpAwardSummary? XpAwarded { get; set; }

    // ADDED: The AI analysis report
    public AcademicAnalysisReport? AnalysisReport { get; set; }
}

public class XpAwardSummary
{
    public int TotalXp { get; set; }
    public int SkillsAffected { get; set; }
    public List<SkillXpAward> SkillAwards { get; set; } = new();
}

public class SkillXpAward
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int XpAwarded { get; set; }
    public int NewTotalXp { get; set; }
    public int NewLevel { get; set; }
    public string SourceSubjectCode { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string TierDescription { get; set; } = string.Empty;
}