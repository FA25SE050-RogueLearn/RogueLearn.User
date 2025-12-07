using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.ApproveJoinRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ApproveJoinRequest;

public class ApproveGuildJoinRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_AddsMemberAndUpdates()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo);

        var cmd = new ApproveGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 50 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        var req = new GuildJoinRequest { Id = cmd.RequestId, GuildId = cmd.GuildId, RequesterId = System.Guid.NewGuid(), Status = GuildJoinRequestStatus.Pending };
        requestRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns(req);

        memberRepo.GetMembershipsByUserAsync(req.RequesterId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());
        guildRepo.GetGuildsByCreatorAsync(req.RequesterId, Arg.Any<CancellationToken>()).Returns(new List<Guild>());
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(10);
        memberRepo.GetMemberAsync(cmd.GuildId, req.RequesterId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembersByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new() { AuthUserId = req.RequesterId, Status = MemberStatus.Active } });
        requestRepo.GetRequestsByRequesterAsync(req.RequesterId, Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequest>());

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received(1).AddAsync(Arg.Any<GuildMember>(), Arg.Any<CancellationToken>());
        await requestRepo.Received(1).UpdateAsync(Arg.Is<GuildJoinRequest>(r => r.Status == GuildJoinRequestStatus.Accepted), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GuildNotFound_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo);

        var cmd = new ApproveGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RequestNotFound_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo);

        var cmd = new ApproveGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 50 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        requestRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns((GuildJoinRequest?)null);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RequesterBelongsToOtherGuild_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo);

        var cmd = new ApproveGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var req = new GuildJoinRequest { Id = cmd.RequestId, GuildId = cmd.GuildId, RequesterId = Guid.NewGuid(), Status = GuildJoinRequestStatus.Pending };
        requestRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns(req);
        memberRepo.GetMembershipsByUserAsync(req.RequesterId, Arg.Any<CancellationToken>())
            .Returns(new List<GuildMember> { new() { GuildId = Guid.NewGuid(), AuthUserId = req.RequesterId, Status = MemberStatus.Active } });
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_JoinRequestGuildMismatch_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo);

        var cmd = new ApproveGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        var req = new GuildJoinRequest { Id = cmd.RequestId, GuildId = Guid.NewGuid(), RequesterId = Guid.NewGuid(), Status = GuildJoinRequestStatus.Pending };
        requestRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns(req);

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RankingAndDeclines_Applied()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo);

        var cmd = new ApproveGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 50 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        var req = new GuildJoinRequest { Id = cmd.RequestId, GuildId = cmd.GuildId, RequesterId = Guid.NewGuid(), Status = GuildJoinRequestStatus.Pending };
        requestRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns(req);
        memberRepo.GetMembershipsByUserAsync(req.RequesterId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(1);
        memberRepo.GetMemberAsync(cmd.GuildId, req.RequesterId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);

        var members = new List<GuildMember>
        {
            new() { GuildId = cmd.GuildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Active, ContributionPoints = 1, JoinedAt = DateTimeOffset.UtcNow.AddDays(-3) },
            new() { GuildId = cmd.GuildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Left }
        };
        memberRepo.GetMembersByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(members);
        requestRepo.GetRequestsByRequesterAsync(req.RequesterId, Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequest>
        {
            new() { Id = Guid.NewGuid(), GuildId = Guid.NewGuid(), RequesterId = req.RequesterId, Status = GuildJoinRequestStatus.Pending }
        });
        invitationRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GuildInvitation, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GuildInvitation> { new() { Id = Guid.NewGuid(), InviteeId = req.RequesterId, Status = InvitationStatus.Pending } });

        await sut.Handle(cmd, CancellationToken.None);

        await memberRepo.Received(1).UpdateRangeAsync(Arg.Is<IEnumerable<GuildMember>>(list => list.Any(m => m.Status != MemberStatus.Active && m.RankWithinGuild == null)), Arg.Any<CancellationToken>());
        await requestRepo.Received(1).DeleteRangeAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
        await invitationRepo.Received(1).UpdateRangeAsync(Arg.Is<IEnumerable<GuildInvitation>>(inv => inv.All(i => i.Status == InvitationStatus.Declined)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CapacityFull_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo);

        var cmd = new ApproveGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 10 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        var req = new GuildJoinRequest { Id = cmd.RequestId, GuildId = cmd.GuildId, RequesterId = System.Guid.NewGuid(), Status = GuildJoinRequestStatus.Pending };
        requestRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns(req);

        memberRepo.GetMembershipsByUserAsync(req.RequesterId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(10);

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }

    [Fact]
    public async Task Handle_InvalidJoinRequestStatus_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo);

        var cmd = new ApproveGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var guild = new Guild { Id = cmd.GuildId, MaxMembers = 50 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        var req = new GuildJoinRequest { Id = cmd.RequestId, GuildId = cmd.GuildId, RequesterId = System.Guid.NewGuid(), Status = GuildJoinRequestStatus.Declined, ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) };
        requestRepo.GetByIdAsync(cmd.RequestId, Arg.Any<CancellationToken>()).Returns(req);

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }
}
