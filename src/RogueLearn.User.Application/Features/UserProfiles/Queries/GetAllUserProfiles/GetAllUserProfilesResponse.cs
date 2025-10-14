using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;

namespace RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;

public class GetAllUserProfilesResponse
{
    public List<UserProfileDto> UserProfiles { get; set; } = new();
}