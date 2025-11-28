using System.Collections.Generic;

namespace RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;

public class FullUserInfoSocialResponse
{
    public ProfileSection Profile { get; set; } = new();
    public AuthSection Auth { get; set; } = new();
    public SocialRelationsSection Relations { get; set; } = new();
    public CountsSection Counts { get; set; } = new();
}

public class SocialRelationsSection
{
    public List<UserRoleItem> UserRoles { get; set; } = new();
    public List<StudentEnrollmentItem> StudentEnrollments { get; set; } = new();
    public List<UserSkillItem> UserSkills { get; set; } = new();
    public List<UserAchievementItem> UserAchievements { get; set; } = new();
    public List<PartyMemberItem> PartyMembers { get; set; } = new();
    public List<GuildMemberItem> GuildMembers { get; set; } = new();
    public List<QuestAttemptItem> QuestAttempts { get; set; } = new();
}