namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminGetLecturerVerificationRequest;

public class AdminLecturerVerificationRequestDetail
{
    public Guid Id { get; set; }
    public Guid AuthUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty;
    public string? ScreenshotUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
}