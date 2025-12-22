using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord; // Reuse XpAwardSummary

namespace RogueLearn.User.Application.Features.Student.Commands.UpdateSingleSubjectGrade;

public class UpdateSingleSubjectGradeResponse
{
    public Guid SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string NewGrade { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public XpAwardSummary? XpAwarded { get; set; }
}