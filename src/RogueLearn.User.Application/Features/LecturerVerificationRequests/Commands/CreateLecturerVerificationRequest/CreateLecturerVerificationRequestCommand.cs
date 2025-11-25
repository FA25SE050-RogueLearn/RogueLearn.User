using MediatR;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.CreateLecturerVerificationRequest;

public class CreateLecturerVerificationRequestCommand : IRequest<CreateLecturerVerificationRequestResponse>
{
    public Guid AuthUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty;
    public string? ScreenshotUrl { get; set; }
}