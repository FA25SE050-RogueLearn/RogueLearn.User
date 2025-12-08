using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.InviteMember;

public class InviteGuildMembersCommandHandlerTests
{
    private static InviteGuildMembersCommandHandler CreateSut(
        IGuildInvitationRepository? invRepo = null,
        IUserProfileRepository? userRepo = null,
        IGuildRepository? guildRepo = null,
        IGuildMemberRepository? memberRepo = null,
        IRoleRepository? roleRepo = null,
        IUserRoleRepository? userRoleRepo = null,
        IGuildNotificationService? notify = null)
    {
        invRepo ??= Substitute.For<IGuildInvitationRepository>();
        userRepo ??= Substitute.For<IUserProfileRepository>();
        guildRepo ??= Substitute.For<IGuildRepository>();
        memberRepo ??= Substitute.For<IGuildMemberRepository>();
        roleRepo ??= Substitute.For<IRoleRepository>();
        userRoleRepo ??= Substitute.For<IUserRoleRepository>();
        notify ??= Substitute.For<IGuildNotificationService>();
        return new InviteGuildMembersCommandHandler(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
    }

    [Fact]
    public async Task NotVerifiedLecturer_UnderCap_PassesPrecheckButNoTargets()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        var masterId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 100, CurrentMemberCount = 10 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = masterId } });
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(masterId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("No valid invite targets.");
    }

    [Fact]
    public async Task ExistingPendingValidInvitation_ThrowsAlreadyPending()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var invitee = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());

        var existing = new GuildInvitation { GuildId = guildId, InviteeId = invitee, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        invRepo.GetByGuildAndInviteeAsync(guildId, invitee, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, inviter, new[] { new InviteTarget(invitee, null) }, "");

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("An invitation is already pending for this user.");
    }
    [Fact]
    public async Task VerifiedLecturerCapExceeded_ThrowsUnprocessable()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 100, CurrentMemberCount = 50 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole>());

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, Guid.NewGuid(), new InviteTarget[0], "");

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.UnprocessableEntityException>();
    }

    [Fact]
    public async Task EmptyTargets_ThrowsNoValidInviteTargets()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, Guid.NewGuid(), Array.Empty<InviteTarget>(), "msg");

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("No valid invite targets.");
    }

    [Fact]
    public async Task TargetMissingIdAndEmail_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, Guid.NewGuid(), new[] { new InviteTarget(null, null) }, "msg");

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("Invite target must include userId or email.");
    }

    [Fact]
    public async Task EmailNotFound_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        userRepo.GetByEmailAsync("a@b.c", Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, Guid.NewGuid(), new[] { new InviteTarget(null, "a@b.c") }, "msg");

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("No user found with email 'a@b.c'.");
    }

    [Fact]
    public async Task SelfInvite_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());

        var inviter = Guid.NewGuid();
        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, inviter, new[] { new InviteTarget(inviter, null) }, "");

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("Cannot invite yourself to the guild.");
    }

    [Fact]
    public async Task DuplicatePendingInGuild_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        var invitee = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation> { new GuildInvitation { GuildId = guildId, InviteeId = invitee, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) } });

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, Guid.NewGuid(), new[] { new InviteTarget(invitee, null) }, "");

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("An invitation is already pending for this user.");
    }

    [Fact]
    public async Task MembershipConstraints_Throws()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        var invitee = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        memberRepo.GetMembershipsByUserAsync(invitee, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = Guid.NewGuid(), Status = MemberStatus.Active } });

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, Guid.NewGuid(), new[] { new InviteTarget(invitee, null) }, "");

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task UpdateExistingExpiredInvitation_Notifies()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var invitee = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        var existing = new GuildInvitation { GuildId = guildId, InviteeId = invitee, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) };
        invRepo.GetByGuildAndInviteeAsync(guildId, invitee, Arg.Any<CancellationToken>()).Returns(existing);
        invRepo.UpdateAsync(Arg.Any<GuildInvitation>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildInvitation>());

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, inviter, new[] { new InviteTarget(invitee, null) }, "msg");

        await sut.Handle(cmd, CancellationToken.None);
        await invRepo.Received(1).UpdateAsync(Arg.Is<GuildInvitation>(i => i.Status == InvitationStatus.Pending && i.InviterId == inviter && i.InvitationType == InvitationType.Invite), Arg.Any<CancellationToken>());
        await notify.Received(1).NotifyInvitationCreatedAsync(Arg.Any<GuildInvitation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNewInvitation_Notifies()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var invitee = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());
        invRepo.GetByGuildAndInviteeAsync(guildId, invitee, Arg.Any<CancellationToken>()).Returns((GuildInvitation?)null);
        invRepo.AddAsync(Arg.Any<GuildInvitation>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildInvitation>());

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, inviter, new[] { new InviteTarget(invitee, null) }, "msg");

        var res = await sut.Handle(cmd, CancellationToken.None);
        await invRepo.Received(1).AddAsync(Arg.Is<GuildInvitation>(i => i.GuildId == guildId && i.InviterId == inviter && i.InviteeId == invitee && i.Status == InvitationStatus.Pending), Arg.Any<CancellationToken>());
        await notify.Received(1).NotifyInvitationCreatedAsync(Arg.Any<GuildInvitation>(), Arg.Any<CancellationToken>());
        res.InvitationIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EmailMapsToUser_CreatesInvitation()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var notify = Substitute.For<IGuildNotificationService>();

        var guildId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var inviteeAuthId = Guid.NewGuid();

        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 10, CurrentMemberCount = 5 });
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new GuildMember { GuildId = guildId, Role = GuildRole.GuildMaster, Status = MemberStatus.Active, AuthUserId = Guid.NewGuid() } });
        var verifiedRoleId = Guid.NewGuid();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = verifiedRoleId, Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = verifiedRoleId } });
        invRepo.GetPendingInvitationsByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildInvitation>());

        userRepo.GetByEmailAsync("invite@x.com", Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = inviteeAuthId });
        invRepo.GetByGuildAndInviteeAsync(guildId, inviteeAuthId, Arg.Any<CancellationToken>()).Returns((GuildInvitation?)null);
        invRepo.AddAsync(Arg.Any<GuildInvitation>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildInvitation>());

        var sut = CreateSut(invRepo, userRepo, guildRepo, memberRepo, roleRepo, userRoleRepo, notify);
        var cmd = new InviteGuildMembersCommand(guildId, inviter, new[] { new InviteTarget(null, "invite@x.com") }, "msg");

        var res = await sut.Handle(cmd, CancellationToken.None);
        await invRepo.Received(1).AddAsync(Arg.Is<GuildInvitation>(i => i.InviteeId == inviteeAuthId && i.InviterId == inviter && i.GuildId == guildId), Arg.Any<CancellationToken>());
        res.InvitationIds.Should().NotBeEmpty();
    }
}
