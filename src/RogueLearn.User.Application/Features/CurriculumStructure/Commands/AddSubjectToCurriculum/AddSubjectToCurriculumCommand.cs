using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Commands.AddSubjectToCurriculum;

public class AddSubjectToCurriculumCommand : IRequest<AddSubjectToCurriculumResponse>
{
    public Guid CurriculumVersionId { get; set; }
    public Guid SubjectId { get; set; }
    public int TermNumber { get; set; }
    public bool IsMandatory { get; set; } = true;
    public Guid[]? PrerequisiteSubjectIds { get; set; }
    public string? PrerequisitesText { get; set; }
}