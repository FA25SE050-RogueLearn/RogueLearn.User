using MediatR;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;

namespace RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;

public class UpdateMyProfileCommand : IRequest<UserProfileDto>
{
    public Guid AuthUserId { get; set; }

    // Allowed fields
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public string? PreferencesJson { get; set; }

    public Guid? ClassId { get; set; }
    public Guid? RouteId { get; set; }

    // Optional uploaded image (when using multipart/form-data)
    public byte[]? ProfileImageBytes { get; set; }
    public string? ProfileImageContentType { get; set; }
    public string? ProfileImageFileName { get; set; }
}