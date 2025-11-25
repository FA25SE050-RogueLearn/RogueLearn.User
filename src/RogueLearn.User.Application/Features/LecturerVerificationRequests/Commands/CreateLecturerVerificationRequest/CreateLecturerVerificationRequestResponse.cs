namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.CreateLecturerVerificationRequest;

public class CreateLecturerVerificationRequestResponse
{
    public Guid RequestId { get; set; }
    public string Status { get; set; } = "pending";
}