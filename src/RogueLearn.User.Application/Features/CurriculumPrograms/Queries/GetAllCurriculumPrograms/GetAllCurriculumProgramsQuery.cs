using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;

public class GetAllCurriculumProgramsQuery : IRequest<List<CurriculumProgramDto>>
{
}