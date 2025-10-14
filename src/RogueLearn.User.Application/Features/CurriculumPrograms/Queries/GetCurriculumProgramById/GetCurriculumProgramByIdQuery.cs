using MediatR;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramById;

public class GetCurriculumProgramByIdQuery : IRequest<CurriculumProgramDto>
{
    public Guid Id { get; set; }
}