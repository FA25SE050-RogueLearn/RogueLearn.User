using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

public class GetCurriculumProgramDetailsQuery : IRequest<CurriculumProgramDetailsResponse>
{
    public Guid? ProgramId { get; set; }
    public Guid? VersionId { get; set; }
}