using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionsByProgram;

public class GetCurriculumVersionsByProgramQuery : IRequest<List<CurriculumVersionDto>>
{
    public Guid ProgramId { get; set; }
}