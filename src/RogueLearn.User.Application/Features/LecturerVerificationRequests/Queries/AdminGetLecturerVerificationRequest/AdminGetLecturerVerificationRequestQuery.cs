using MediatR;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminGetLecturerVerificationRequest;

public class AdminGetLecturerVerificationRequestQuery : IRequest<AdminLecturerVerificationRequestDetail?>
{
    public Guid RequestId { get; set; }
}