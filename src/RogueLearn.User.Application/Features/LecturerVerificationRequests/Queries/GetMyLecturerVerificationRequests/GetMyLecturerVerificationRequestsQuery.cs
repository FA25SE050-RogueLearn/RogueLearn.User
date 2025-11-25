using MediatR;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.GetMyLecturerVerificationRequests;

public class GetMyLecturerVerificationRequestsQuery : IRequest<IReadOnlyList<MyLecturerVerificationRequestDto>>
{
    public Guid AuthUserId { get; set; }
}