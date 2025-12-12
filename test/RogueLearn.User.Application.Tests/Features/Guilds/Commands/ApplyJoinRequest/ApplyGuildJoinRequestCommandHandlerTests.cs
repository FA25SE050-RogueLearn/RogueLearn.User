using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.ApplyJoinRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ApplyJoinRequest;

public class ApplyGuildJoinRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_PublicGuild_AssignsRanks_OrdersAndLoops()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var guild = new Guild { Id = guildId, MaxMembers = 100, IsPublic = true, RequiresApproval = false };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(0);
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new GuildMember { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Active, ContributionPoints = 5, JoinedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            new GuildMember { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Active, ContributionPoints = 10, JoinedAt = DateTimeOffset.UtcNow.AddDays(-3) },
            new GuildMember { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Inactive }
        });

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        IEnumerable<GuildMember>? captured = null;
        memberRepo.UpdateRangeAsync(Arg.Any<IEnumerable<GuildMember>>(), Arg.Any<CancellationToken>())
                  .Returns(ci => {
                      captured = ci.ArgAt<IEnumerable<GuildMember>>(0);
                      return Task.FromResult(captured!);
                  });

        await sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);

        captured.Should().NotBeNull();
        var activeRanks = captured!.Where(m => m.Status == MemberStatus.Active).OrderByDescending(m => m.ContributionPoints).ThenBy(m => m.JoinedAt).Select(m => m.RankWithinGuild).ToList();
        activeRanks.Should().BeEquivalentTo(new[] { 1, 2 });
        captured.Any(m => m.Status != MemberStatus.Active && m.RankWithinGuild == null).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PublicGuild_DeclinesPendingInvites()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var guild = new Guild { Id = guildId, MaxMembers = 100, IsPublic = true, RequiresApproval = false };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(0);
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());

        var pendingInv1 = new GuildInvitation { Id = Guid.NewGuid(), InviteeId = authId, Status = InvitationStatus.Pending };
        var pendingInv2 = new GuildInvitation { Id = Guid.NewGuid(), InviteeId = authId, Status = InvitationStatus.Pending };
        inviteRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GuildInvitation, bool>>>(), Arg.Any<CancellationToken>())
                  .Returns(new[] { pendingInv1, pendingInv2 });

        IEnumerable<GuildInvitation>? updated = null;
        inviteRepo.UpdateRangeAsync(Arg.Any<IEnumerable<GuildInvitation>>(), Arg.Any<CancellationToken>())
                  .Returns(ci => { updated = ci.ArgAt<IEnumerable<GuildInvitation>>(0); return Task.FromResult(updated!); });

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        await sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.All(i => i.Status == InvitationStatus.Declined && i.RespondedAt.HasValue).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PublicGuild_ExistingRequest_DeclinesPendingInvites()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var guild = new Guild { Id = guildId, MaxMembers = 100, IsPublic = true, RequiresApproval = false };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(0);
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());

        var existingReq = new GuildJoinRequest { GuildId = guildId, Status = GuildJoinRequestStatus.Declined };
        joinRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>()).Returns(new[] { existingReq });
        joinRepo.UpdateAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        var pendingInv1 = new GuildInvitation { Id = Guid.NewGuid(), InviteeId = authId, Status = InvitationStatus.Pending };
        var pendingInv2 = new GuildInvitation { Id = Guid.NewGuid(), InviteeId = authId, Status = InvitationStatus.Pending };
        inviteRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GuildInvitation, bool>>>(), Arg.Any<CancellationToken>())
                  .Returns(new[] { pendingInv1, pendingInv2 });

        IEnumerable<GuildInvitation>? updated = null;
        inviteRepo.UpdateRangeAsync(Arg.Any<IEnumerable<GuildInvitation>>(), Arg.Any<CancellationToken>())
                  .Returns(ci => { updated = ci.ArgAt<IEnumerable<GuildInvitation>>(0); return Task.FromResult(updated!); });

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        await sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.All(i => i.Status == InvitationStatus.Declined && i.RespondedAt.HasValue).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PrivateGuild_ExistingNonPending_UpdatesToPending_AndNotifiesMaster()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var guild = new Guild { Id = guildId, MaxMembers = 100, IsPublic = false, RequiresApproval = true };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(0);
        var masterId = Guid.NewGuid();
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new[] { new GuildMember { GuildId = guildId, Status = MemberStatus.Active, Role = GuildRole.GuildMaster, AuthUserId = masterId } });

        var existingReq = new GuildJoinRequest { GuildId = guildId, Status = GuildJoinRequestStatus.Declined };
        joinRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>()).Returns(new[] { existingReq });
        joinRepo.UpdateAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        GuildJoinRequest? updatedReq = null;
        joinRepo.UpdateAsync(Arg.Do<GuildJoinRequest>(r => updatedReq = r), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        await sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);

        updatedReq.Should().NotBeNull();
        updatedReq!.Status.Should().Be(GuildJoinRequestStatus.Pending);
        await notify.Received(1).NotifyJoinRequestSubmittedAsync(masterId, Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GuildNotFound_ThrowsNotFound()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        var act = () => sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyMember_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 100, IsPublic = true, RequiresApproval = false });
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns(new GuildMember { GuildId = guildId, AuthUserId = authId, Status = MemberStatus.Active });

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        var act = () => sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_BelongsOtherGuild_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 100, IsPublic = true, RequiresApproval = false });
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new[] { new GuildMember { GuildId = Guid.NewGuid(), Status = MemberStatus.Active } });

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        var act = () => sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_CapExceededWithoutVerifiedLecturer_ThrowsUnprocessable()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var guild = new Guild { Id = guildId, MaxMembers = 100, IsPublic = true, RequiresApproval = false };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(50);
        var masterId = Guid.NewGuid();
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new[] { new GuildMember { GuildId = guildId, Status = MemberStatus.Active, Role = GuildRole.GuildMaster, AuthUserId = masterId } });
        var verifiedRole = new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedRole);
        userRoleRepo.GetRolesForUserAsync(masterId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserRole>());

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        var act = () => sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.UnprocessableEntityException>();
    }

    [Fact]
    public async Task Handle_ExistingPendingRequest_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 100, IsPublic = false, RequiresApproval = true });
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(0);
        joinRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>()).Returns(new[] { new GuildJoinRequest { GuildId = guildId, Status = GuildJoinRequestStatus.Pending } });

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        var act = () => sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_PrivateGuild_CreatesPendingAndNotifiesMaster()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var guild = new Guild { Id = guildId, MaxMembers = 100, IsPublic = false, RequiresApproval = true };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(0);
        joinRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildJoinRequest>());
        var masterId = Guid.NewGuid();
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new[] { new GuildMember { GuildId = guildId, Status = MemberStatus.Active, Role = GuildRole.GuildMaster, AuthUserId = masterId } });

        GuildJoinRequest? created = null;
        joinRepo.AddAsync(Arg.Do<GuildJoinRequest>(r => created = r), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        await sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);

        created.Should().NotBeNull();
        created!.Status.Should().Be(GuildJoinRequestStatus.Pending);
        await notify.Received(1).NotifyJoinRequestSubmittedAsync(masterId, Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublicGuild_ExistingRequestUpdatedAccepted_DeletesOtherPendingRequests()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var guild = new Guild { Id = guildId, MaxMembers = 100, IsPublic = true, RequiresApproval = false };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(0);
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());

        var existingReq = new GuildJoinRequest { GuildId = guildId, Status = GuildJoinRequestStatus.Declined };
        joinRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>()).Returns(new[] { existingReq });
        joinRepo.UpdateAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        var otherIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        joinRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>()).Returns(new[] { existingReq, new GuildJoinRequest { Id = otherIds[0], GuildId = Guid.NewGuid(), Status = GuildJoinRequestStatus.Pending }, new GuildJoinRequest { Id = otherIds[1], GuildId = Guid.NewGuid(), Status = GuildJoinRequestStatus.Pending } });

        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo, roleRepo, userRoleRepo);
        await sut.Handle(new ApplyGuildJoinRequestCommand(guildId, authId, "msg"), CancellationToken.None);

        await joinRepo.Received(1).DeleteRangeAsync(Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(otherIds[0]) && ids.Contains(otherIds[1])), Arg.Any<CancellationToken>());
    }
}
