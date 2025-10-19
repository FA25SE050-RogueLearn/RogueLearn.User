// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumPrograms/Queries/GetCurriculumProgramDetails/GetCurriculumProgramDetailsQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

public class GetCurriculumProgramDetailsQuery : IRequest<CurriculumProgramDetailsResponse>
{
    // MODIFIED: This query can now be initiated with EITHER a ProgramId or a VersionId,
    // making it more flexible for internal clients.
    public Guid? ProgramId { get; set; }
    public Guid? VersionId { get; set; }
}