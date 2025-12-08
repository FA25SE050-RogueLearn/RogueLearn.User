using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.LeaveGuild;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.LeaveGuild;

public class LeaveGuildCommandHandlerTests
{
    [Fact]
    public async Task Handle_MasterWithOthers_Disallowed()
    {
        var cmd = new LeaveGuildCommand(System.Guid.NewGuid(), System.Guid.NewGuid());
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notification = Substitute.For<IGuildNotificationService>();
        var sut = new LeaveGuildCommandHandler(memberRepo, guildRepo, userRoleRepo, roleRepo, notification);

        var master = new GuildMember { GuildId = cmd.GuildId, AuthUserId = cmd.AuthUserId, Role = GuildRole.GuildMaster };
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(master);
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(2);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_DeletesAndUpdates()
    {
        var cmd = new LeaveGuildCommand(System.Guid.NewGuid(), System.Guid.NewGuid());
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notification = Substitute.For<IGuildNotificationService>();
        var sut = new LeaveGuildCommandHandler(memberRepo, guildRepo, userRoleRepo, roleRepo, notification);

        var member = new GuildMember { GuildId = cmd.GuildId, AuthUserId = cmd.AuthUserId, Role = GuildRole.Member };
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(member);
        var guild = new Guild { Id = cmd.GuildId, CurrentMemberCount = 10 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMembersByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received(1).DeleteAsync(member.Id, Arg.Any<CancellationToken>());
        await guildRepo.Received(1).UpdateAsync(Arg.Any<Guild>(), Arg.Any<CancellationToken>());
        await memberRepo.Received(1).UpdateRangeAsync(Arg.Any<IEnumerable<GuildMember>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MemberNotFound_Throws()
    {
        var cmd = new LeaveGuildCommand(System.Guid.NewGuid(), System.Guid.NewGuid());
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notification = Substitute.For<IGuildNotificationService>();
        var sut = new LeaveGuildCommandHandler(memberRepo, guildRepo, userRoleRepo, roleRepo, notification);

        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MasterSoleMember_RemovesRoleAndUpdates()
    {
        var cmd = new LeaveGuildCommand(System.Guid.NewGuid(), System.Guid.NewGuid());
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notification = Substitute.For<IGuildNotificationService>();
        var sut = new LeaveGuildCommandHandler(memberRepo, guildRepo, userRoleRepo, roleRepo, notification);

        var master = new GuildMember { Id = System.Guid.NewGuid(), GuildId = cmd.GuildId, AuthUserId = cmd.AuthUserId, Role = GuildRole.GuildMaster };
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(master);
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(1);

        var gmRole = new Role { Id = System.Guid.NewGuid(), Name = "Guild Master" };
        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns(gmRole);
        var mapping = new UserRole { Id = System.Guid.NewGuid(), AuthUserId = cmd.AuthUserId, RoleId = gmRole.Id };
        userRoleRepo.GetRolesForUserAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { mapping });

        var guild = new Guild { Id = cmd.GuildId };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMembersByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());

        await sut.Handle(cmd, CancellationToken.None);

        await userRoleRepo.Received(1).DeleteAsync(mapping.Id, Arg.Any<CancellationToken>());
        await guildRepo.Received(1).UpdateAsync(Arg.Is<Guild>(g => g.Id == cmd.GuildId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MasterSoleMember_MissingRole_Throws()
    {
        var cmd = new LeaveGuildCommand(System.Guid.NewGuid(), System.Guid.NewGuid());
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notification = Substitute.For<IGuildNotificationService>();
        var sut = new LeaveGuildCommandHandler(memberRepo, guildRepo, userRoleRepo, roleRepo, notification);

        var master = new GuildMember { GuildId = cmd.GuildId, AuthUserId = cmd.AuthUserId, Role = GuildRole.GuildMaster };
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(master);
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(1);
        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns((Role?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GuildNotFoundAfterDelete_Throws()
    {
        var cmd = new LeaveGuildCommand(System.Guid.NewGuid(), System.Guid.NewGuid());
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var notification = Substitute.For<IGuildNotificationService>();
        var sut = new LeaveGuildCommandHandler(memberRepo, guildRepo, userRoleRepo, roleRepo, notification);

        var member = new GuildMember { Id = System.Guid.NewGuid(), GuildId = cmd.GuildId, AuthUserId = cmd.AuthUserId, Role = GuildRole.Member };
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(member);
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
