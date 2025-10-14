namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;

public class ImportSyllabusResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
    public string? SubjectCode { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? SyllabusVersionId { get; set; }
}