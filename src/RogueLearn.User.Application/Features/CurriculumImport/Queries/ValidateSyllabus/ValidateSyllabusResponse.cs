using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

public class ValidateSyllabusResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
    public SyllabusData? ExtractedData { get; set; }
}