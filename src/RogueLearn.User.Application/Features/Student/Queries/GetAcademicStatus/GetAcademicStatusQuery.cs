using MediatR;

namespace RogueLearn.User.Application.Features.Student.Queries.GetAcademicStatus;

public class GetAcademicStatusQuery : IRequest<GetAcademicStatusResponse?>
{
    public Guid AuthUserId { get; set; }
}