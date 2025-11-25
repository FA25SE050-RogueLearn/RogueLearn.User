using MediatR;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminListLecturerVerificationRequests;

public class AdminListLecturerVerificationRequestsQuery : IRequest<AdminListLecturerVerificationRequestsResponse>
{
    public string? Status { get; set; }
    public Guid? UserId { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}