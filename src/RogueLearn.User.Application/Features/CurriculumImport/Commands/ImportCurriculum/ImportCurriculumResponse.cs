namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;

public class ImportCurriculumResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
    public Guid? CurriculumProgramId { get; set; }
    public Guid? CurriculumVersionId { get; set; }
    public List<Guid> SubjectIds { get; set; } = new();
}