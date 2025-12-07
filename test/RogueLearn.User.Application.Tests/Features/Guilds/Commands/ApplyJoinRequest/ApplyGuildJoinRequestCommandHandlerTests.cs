using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.ApplyJoinRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ApplyJoinRequest;

public class ApplyGuildJoinRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_AddsPendingRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var cmd = new ApplyGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), "msg");
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50, RequiresApproval = true, IsPublic = true });
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(10);
        requestRepo.GetRequestsByRequesterAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequest>());
        requestRepo.AddAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        await sut.Handle(cmd, CancellationToken.None);
        await requestRepo.Received(1).AddAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyMember_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "msg");
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 50, RequiresApproval = true, IsPublic = true });
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns(new GuildMember { GuildId = guildId, AuthUserId = authId, Status = MemberStatus.Active });

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActiveInAnotherGuild_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var otherGuildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "msg");
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 50, RequiresApproval = true, IsPublic = true });
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>())
                  .Returns(new List<GuildMember> { new GuildMember { GuildId = otherGuildId, AuthUserId = authId, Status = MemberStatus.Active } });

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PendingRequestForGuild_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "msg");
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 50, RequiresApproval = true, IsPublic = true });
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());
        requestRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>())
                   .Returns(new List<GuildJoinRequest> { new GuildJoinRequest { GuildId = guildId, RequesterId = authId, Status = GuildJoinRequestStatus.Pending } });

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GuildAtCapacity_ThrowsUnprocessableEntity()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "msg");
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 1, RequiresApproval = true, IsPublic = true });
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(1);

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.UnprocessableEntityException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PublicNoApproval_AddsMemberAndAcceptsRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "welcome");
        var guild = new Guild { Id = guildId, MaxMembers = 50, RequiresApproval = false, IsPublic = true };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        requestRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequest>());
        requestRepo.AddAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());
        memberRepo.AddAsync(Arg.Any<GuildMember>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildMember>());
        guildRepo.UpdateAsync(Arg.Any<Guild>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Guild>());

        await sut.Handle(cmd, CancellationToken.None);

        await memberRepo.Received(1).AddAsync(Arg.Is<GuildMember>(m => m.GuildId == guildId && m.AuthUserId == authId && m.Status == MemberStatus.Active), Arg.Any<CancellationToken>());
        await requestRepo.Received(1).AddAsync(Arg.Is<GuildJoinRequest>(r => r.GuildId == guildId && r.RequesterId == authId && r.Status == GuildJoinRequestStatus.Accepted), Arg.Any<CancellationToken>());
        await notificationService.Received(1).NotifyJoinRequestApprovedAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
        await guildRepo.Received(1).UpdateAsync(Arg.Is<Guild>(g => g.Id == guildId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublicNoApproval_DeclinesOtherInvitations()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "msg");
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 50, RequiresApproval = false, IsPublic = true });
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        requestRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequest>());

        var otherInvites = new List<GuildInvitation>
        {
            new() { Id = Guid.NewGuid(), InviteeId = authId, Status = InvitationStatus.Pending },
            new() { Id = Guid.NewGuid(), InviteeId = authId, Status = InvitationStatus.Pending }
        };
        invitationRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GuildInvitation, bool>>>(), Arg.Any<CancellationToken>()).Returns(otherInvites);
        invitationRepo.UpdateRangeAsync(Arg.Any<IEnumerable<GuildInvitation>>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<IEnumerable<GuildInvitation>>());

        await sut.Handle(cmd, CancellationToken.None);

        await invitationRepo.Received(1).UpdateRangeAsync(Arg.Is<IEnumerable<GuildInvitation>>(list => list.All(i => i.Status == InvitationStatus.Declined)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RequiresApproval_Pending_NotifiesGuildMaster()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "please");
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 50, RequiresApproval = true, IsPublic = true });
        requestRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequest>());
        requestRepo.AddAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        var master = new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Role = GuildRole.GuildMaster, Status = MemberStatus.Active };
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { master });

        await sut.Handle(cmd, CancellationToken.None);

        await notificationService.Received(1).NotifyJoinRequestSubmittedAsync(master.AuthUserId, Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GuildNotFound_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var cmd = new ApplyGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), "msg");
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonVerifiedMasterCapReached_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "msg");
        var guild = new Guild { Id = guildId, MaxMembers = 100, RequiresApproval = true, IsPublic = true };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(50);
        var master = new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Role = GuildRole.GuildMaster, Status = MemberStatus.Active };
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { master });
        var verifiedLecturerRole = new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedLecturerRole);
        userRoleRepo.GetRolesForUserAsync(master.AuthUserId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserRole>());

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.UnprocessableEntityException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PublicNoApproval_ExistingRequestUpdated()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "msg");
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 50, RequiresApproval = false, IsPublic = true });
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        requestRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildJoinRequest> { new() { GuildId = guildId, RequesterId = authId, Status = GuildJoinRequestStatus.Declined } });
        requestRepo.UpdateAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        await sut.Handle(cmd, CancellationToken.None);
        await requestRepo.Received(1).UpdateAsync(Arg.Is<GuildJoinRequest>(r => r.GuildId == guildId && r.RequesterId == authId && r.Status == GuildJoinRequestStatus.Accepted), Arg.Any<CancellationToken>());
        await notificationService.Received(1).NotifyJoinRequestApprovedAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RequiresApproval_ExistingRequestUpdatedAndNotified()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "msg");
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 50, RequiresApproval = true, IsPublic = true });
        requestRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildJoinRequest> { new() { GuildId = guildId, RequesterId = authId, Status = GuildJoinRequestStatus.Declined } });
        requestRepo.UpdateAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());
        var master = new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Role = GuildRole.GuildMaster, Status = MemberStatus.Active };
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { master });

        await sut.Handle(cmd, CancellationToken.None);
        await requestRepo.Received(1).UpdateAsync(Arg.Is<GuildJoinRequest>(r => r.GuildId == guildId && r.Status == GuildJoinRequestStatus.Pending), Arg.Any<CancellationToken>());
        await notificationService.Received(1).NotifyJoinRequestSubmittedAsync(master.AuthUserId, Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublicNoApproval_RemovesOtherPendingRequests()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var cmd = new ApplyGuildJoinRequestCommand(guildId, authId, "msg");
        var guild = new Guild { Id = guildId, MaxMembers = 50, RequiresApproval = false, IsPublic = true };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        requestRepo.GetRequestsByRequesterAsync(authId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildJoinRequest> {
                new() { Id = Guid.NewGuid(), GuildId = Guid.NewGuid(), RequesterId = authId, Status = GuildJoinRequestStatus.Pending },
                new() { Id = Guid.NewGuid(), GuildId = Guid.NewGuid(), RequesterId = authId, Status = GuildJoinRequestStatus.Accepted }
            });

        await sut.Handle(cmd, CancellationToken.None);
        await requestRepo.Received(1).DeleteRangeAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }
}
