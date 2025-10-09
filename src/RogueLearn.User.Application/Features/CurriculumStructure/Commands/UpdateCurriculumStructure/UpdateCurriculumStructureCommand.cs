using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Commands.UpdateCurriculumStructure;

public class UpdateCurriculumStructureCommand : IRequest<UpdateCurriculumStructureResponse>
{
    public Guid Id { get; set; }
    public int TermNumber { get; set; }
    public bool IsMandatory { get; set; }
    public Guid[]? PrerequisiteSubjectIds { get; set; }
    public string? PrerequisitesText { get; set; }
}