using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.LeaveGuild;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.LeaveGuild;

public class LeaveGuildCommandHandlerTests
{
    [Fact]
    public async Task GuildMaster_WithOtherMembers_ThrowsBadRequest()
    {
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();
        var gid = Guid.NewGuid();
        var uid = Guid.NewGuid();
        memberRepo.GetMemberAsync(gid, uid, Arg.Any<CancellationToken>()).Returns(new GuildMember { Id = Guid.NewGuid(), GuildId = gid, AuthUserId = uid, Role = GuildRole.GuildMaster, Status = MemberStatus.Active });
        memberRepo.CountActiveMembersAsync(gid, Arg.Any<CancellationToken>()).Returns(2);
        var sut = new LeaveGuildCommandHandler(memberRepo, guildRepo, userRoleRepo, roleRepo, notify);
        var act = () => sut.Handle(new LeaveGuildCommand(gid, uid), CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("GuildMaster cannot leave while other members exist*");
    }

    [Fact]
    public async Task Member_Leaves_RecalculatesRanks_AndNotifiesMaster()
    {
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();
        var gid = Guid.NewGuid();
        var uid = Guid.NewGuid();
        var masterId = Guid.NewGuid();
        var leaving = new GuildMember { Id = Guid.NewGuid(), GuildId = gid, AuthUserId = uid, Role = GuildRole.Member, Status = MemberStatus.Active };
        memberRepo.GetMemberAsync(gid, uid, Arg.Any<CancellationToken>()).Returns(leaving);
        var guild = new Guild { Id = gid, Name = "G" };
        guildRepo.GetByIdAsync(gid, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.CountActiveMembersAsync(gid, Arg.Any<CancellationToken>()).Returns(1, 0);
        var m1 = new GuildMember { Id = Guid.NewGuid(), GuildId = gid, AuthUserId = masterId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, ContributionPoints = 50, JoinedAt = DateTimeOffset.UtcNow.AddDays(-10) };
        var m2 = new GuildMember { Id = Guid.NewGuid(), GuildId = gid, AuthUserId = Guid.NewGuid(), Role = GuildRole.Member, Status = MemberStatus.Active, ContributionPoints = 20, JoinedAt = DateTimeOffset.UtcNow.AddDays(-5) };
        var m3 = new GuildMember { Id = Guid.NewGuid(), GuildId = gid, AuthUserId = Guid.NewGuid(), Role = GuildRole.Member, Status = MemberStatus.Inactive, ContributionPoints = 100 };
        memberRepo.GetMembersByGuildAsync(gid, Arg.Any<CancellationToken>()).Returns(new[] { m1, m2, m3 });
        List<GuildMember> updated = new();
        memberRepo.UpdateRangeAsync(Arg.Do<IEnumerable<GuildMember>>(x => updated = x.ToList()), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<IEnumerable<GuildMember>>()));
        var sut = new LeaveGuildCommandHandler(memberRepo, guildRepo, userRoleRepo, roleRepo, notify);
        await sut.Handle(new LeaveGuildCommand(gid, uid), CancellationToken.None);
        updated.Should().Contain(m1).And.Contain(m2).And.Contain(m3);
        m1.RankWithinGuild.Should().Be(1);
        m2.RankWithinGuild.Should().Be(2);
        m3.RankWithinGuild.Should().BeNull();
        await notify.Received(1).NotifyMemberLeftAsync(gid, uid, masterId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GuildMaster_SoleMember_Leaves_RemovesGuildMasterRole()
    {
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();
        var gid = Guid.NewGuid();
        var uid = Guid.NewGuid();
        var leaving = new GuildMember { Id = Guid.NewGuid(), GuildId = gid, AuthUserId = uid, Role = GuildRole.GuildMaster, Status = MemberStatus.Active };
        memberRepo.GetMemberAsync(gid, uid, Arg.Any<CancellationToken>()).Returns(leaving);
        memberRepo.CountActiveMembersAsync(gid, Arg.Any<CancellationToken>()).Returns(1, 0);
        var guild = new Guild { Id = gid, Name = "G" };
        guildRepo.GetByIdAsync(gid, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMembersByGuildAsync(gid, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        var gmRole = new Role { Id = Guid.NewGuid(), Name = "Guild Master" };
        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns(gmRole);
        var ur1 = new UserRole { Id = Guid.NewGuid(), AuthUserId = uid, RoleId = gmRole.Id };
        var ur2 = new UserRole { Id = Guid.NewGuid(), AuthUserId = uid, RoleId = Guid.NewGuid() };
        userRoleRepo.GetRolesForUserAsync(uid, Arg.Any<CancellationToken>()).Returns(new[] { ur1, ur2 });
        var sut = new LeaveGuildCommandHandler(memberRepo, guildRepo, userRoleRepo, roleRepo, notify);
        await sut.Handle(new LeaveGuildCommand(gid, uid), CancellationToken.None);
        await userRoleRepo.Received().DeleteAsync(ur1.Id, Arg.Any<CancellationToken>());
    }
}
