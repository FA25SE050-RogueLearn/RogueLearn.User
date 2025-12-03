using FluentAssertions;
using RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;

namespace RogueLearn.User.Application.Tests.Features.UserFullInfo.Queries.GetFullUserInfo;

public class FullUserInfoSocialResponseTests
{
    [Fact]
    public void Can_Instantiate_And_Set_Collections()
    {
        var res = new FullUserInfoSocialResponse();
        res.Relations.UserRoles.Add(new UserRoleItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Admin"));
        res.Relations.StudentEnrollments.Add(new StudentEnrollmentItem(Guid.NewGuid(), "Active", new DateOnly(2024, 9, 1), new DateOnly(2028, 6, 1)));
        res.Relations.UserSkills.Add(new UserSkillItem(Guid.NewGuid(), "Algo", 2, 150));
        res.Relations.UserAchievements.Add(new UserAchievementItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Achv", null));
        res.Relations.PartyMembers.Add(new PartyMemberItem(Guid.NewGuid(), "Party", "Member", DateTimeOffset.UtcNow));
        res.Relations.GuildMembers.Add(new GuildMemberItem(Guid.NewGuid(), "Guild", "Member", DateTimeOffset.UtcNow));
        res.Relations.QuestAttempts.Add(new QuestAttemptItem(Guid.NewGuid(), Guid.NewGuid(), "Quest", "InProgress", 0.5m, 100, DateTimeOffset.UtcNow, null, 10, 2, null));
        res.Relations.UserRoles.Count.Should().Be(1);
        res.Counts.Should().NotBeNull();
    }
}