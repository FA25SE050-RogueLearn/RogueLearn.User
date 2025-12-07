using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.InviteMember;

public class InviteGuildMembersCommandHandlerTests
{
    [Fact]
    public async Task Handle_SelfInvite_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var target = new InviteTarget(cmd.InviterAuthUserId, null);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmailResolvesAndCreates()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var invitee = new UserProfile { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Email = "target@example.com" };
        userRepo.GetByEmailAsync(invitee.Email, Arg.Any<CancellationToken>()).Returns(invitee);
        invRepo.AddAsync(Arg.Any<GuildInvitation>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildInvitation>());

        var target = new InviteTarget(null, invitee.Email);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        var resp = await sut.Handle(cmd, CancellationToken.None);
        Assert.True(resp.InvitationIds.Count >= 1);
    }

    [Fact]
    public async Task Handle_AlreadyMember_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var invitee = new UserProfile { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Email = "member@example.com" };
        userRepo.GetByEmailAsync(invitee.Email, Arg.Any<CancellationToken>()).Returns(invitee);
        memberRepo.GetMembershipsByUserAsync(invitee.AuthUserId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildMember> { new() { GuildId = cmd.GuildId, AuthUserId = invitee.AuthUserId, Status = MemberStatus.Active } });

        var target = new InviteTarget(null, invitee.Email);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GuildNotFound_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonVerifiedMasterCapReached_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 100, CurrentMemberCount = 50 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);

        var master = new GuildMember { GuildId = cmd.GuildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Active, Role = GuildRole.GuildMaster };
        memberRepo.GetMembersByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { master });
        var verifiedLecturerRole = new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedLecturerRole);
        userRoleRepo.GetRolesForUserAsync(master.AuthUserId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserRole>());

        await Assert.ThrowsAsync<UnprocessableEntityException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TargetMissingUserIdAndEmail_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });

        var target = new InviteTarget(null, " ");
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmailNotFound_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });

        var email = "missing@example.com";
        userRepo.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var target = new InviteTarget(null, email);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PendingInvitationExists_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        var inviteeId = Guid.NewGuid();
        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildInvitation> { new() { GuildId = cmd.GuildId, InviteeId = inviteeId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) } });
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });

        var target = new InviteTarget(inviteeId, null);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_BelongsToOtherGuild_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var inviteeId = Guid.NewGuid();
        memberRepo.GetMembershipsByUserAsync(inviteeId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildMember> { new() { GuildId = Guid.NewGuid(), AuthUserId = inviteeId, Status = MemberStatus.Active } });

        var target = new InviteTarget(inviteeId, null);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingInvitationPendingNotExpired_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var inviteeId = Guid.NewGuid();
        invRepo.GetByGuildAndInviteeAsync(cmd.GuildId, inviteeId, Arg.Any<CancellationToken>())
            .Returns(new GuildInvitation { GuildId = cmd.GuildId, InviteeId = inviteeId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });

        var target = new InviteTarget(inviteeId, null);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingInvitationUpdated_ReturnsIdAndNotifies()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        invRepo.GetPendingInvitationsByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var inviteeId = Guid.NewGuid();
        var existing = new GuildInvitation { Id = Guid.NewGuid(), GuildId = cmd.GuildId, InviteeId = inviteeId, Status = InvitationStatus.Declined, ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) };
        invRepo.GetByGuildAndInviteeAsync(cmd.GuildId, inviteeId, Arg.Any<CancellationToken>()).Returns(existing);
        invRepo.UpdateAsync(existing, Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildInvitation>());

        var target = new InviteTarget(inviteeId, null);
        cmd = new InviteGuildMembersCommand(cmd.GuildId, cmd.InviterAuthUserId, new[] { target }, "msg");
        var resp = await sut.Handle(cmd, CancellationToken.None);
        Assert.Contains(existing.Id, resp.InvitationIds);
        await notificationService.Received(1).NotifyInvitationCreatedAsync(Arg.Is<GuildInvitation>(gi => gi.Id == existing.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyTargets_ThrowsNoValidInviteTargets()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notificationService);

        var cmd = new InviteGuildMembersCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
