using MediatR;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.ApproveLecturerVerificationRequest;

public class ApproveLecturerVerificationRequestCommand : IRequest
{
    public Guid RequestId { get; set; }
    public Guid ReviewerAuthUserId { get; set; }
    public string? Note { get; set; }
}