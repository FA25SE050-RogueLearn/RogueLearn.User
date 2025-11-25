namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.GetMyLecturerVerificationRequests;

public class MyLecturerVerificationRequestDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ScreenshotUrl { get; set; }
    public Dictionary<string, object>? Documents { get; set; }
}