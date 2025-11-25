using Microsoft.AspNetCore.Http;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests;

public class CreateLecturerVerificationFormData
{
    public string Email { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty;
    public IFormFile? Screenshot { get; set; }
}