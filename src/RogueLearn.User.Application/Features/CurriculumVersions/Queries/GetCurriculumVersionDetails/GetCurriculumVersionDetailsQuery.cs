// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumVersions/Queries/GetCurriculumVersionDetails/GetCurriculumVersionDetailsQuery.cs
using MediatR;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionDetails;

public class GetCurriculumVersionDetailsQuery : IRequest<CurriculumVersionDetailsDto>
{
    public Guid CurriculumVersionId { get; set; }
}