// RogueLearn.User/src/RogueLearn.User.Application/Features/UserProfiles/Queries/GetUserProfileByAuthId/UserProfileDto.cs
namespace RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid AuthUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int Level { get; set; }
    public int ExperiencePoints { get; set; }
    public bool OnboardingCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}