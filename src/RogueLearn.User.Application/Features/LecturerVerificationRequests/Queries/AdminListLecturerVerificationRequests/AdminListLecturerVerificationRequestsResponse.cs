namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminListLecturerVerificationRequests;

public class AdminListLecturerVerificationRequestsResponse
{
    public List<AdminLecturerVerificationItem> Items { get; set; } = new();
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
}

public class AdminLecturerVerificationItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Institution { get; set; }
}