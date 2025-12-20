namespace RogueLearn.User.Application.Models;

public class AcademicAnalysisReport
{
    public string StudentPersona { get; set; } = string.Empty; // e.g. "Backend Specialist", "Theoretical Scholar"
    public List<string> StrongAreas { get; set; } = new();
    public List<string> WeakAreas { get; set; } = new();
    public string Recommendations { get; set; } = string.Empty;
}