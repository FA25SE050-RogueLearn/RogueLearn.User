using System.Collections.Generic;

namespace RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;

public class FullUserInfoSocialResponse
{
    public ProfileSection Profile { get; set; } = new();
    public AuthSection Auth { get; set; } = new();
    public RelationsSection Relations { get; set; } = new();
    public CountsSection Counts { get; set; } = new();
}