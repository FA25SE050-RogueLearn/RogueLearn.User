using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;

public class ValidateCurriculumResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
    public CurriculumImportData? ExtractedData { get; set; }
}