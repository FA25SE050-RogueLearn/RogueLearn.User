using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;

public class ImportCurriculumCommand : IRequest<ImportCurriculumResponse>
{
    public string RawText { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
}