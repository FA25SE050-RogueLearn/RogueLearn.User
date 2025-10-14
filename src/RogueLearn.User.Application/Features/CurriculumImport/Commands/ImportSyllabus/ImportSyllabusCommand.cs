using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;

public class ImportSyllabusCommand : IRequest<ImportSyllabusResponse>
{
    public string RawText { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
}