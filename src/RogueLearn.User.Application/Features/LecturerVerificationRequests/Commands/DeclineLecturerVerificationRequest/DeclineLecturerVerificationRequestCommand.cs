using MediatR;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.DeclineLecturerVerificationRequest;

public class DeclineLecturerVerificationRequestCommand : IRequest
{
    public Guid RequestId { get; set; }
    public Guid ReviewerAuthUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}