using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.ApproveJoinRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ApproveJoinRequest;

public class ApproveGuildJoinRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_AssignsRanks_OrderByDescendingThenBy()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();

        var guildId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        var guild = new Guild { Id = guildId, MaxMembers = 100 };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);

        var joinReq = new GuildJoinRequest { Id = reqId, GuildId = guildId, RequesterId = requesterId, Status = GuildJoinRequestStatus.Pending, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1), ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        joinRepo.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(joinReq);

        memberRepo.GetMembershipsByUserAsync(requesterId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(0);
        memberRepo.GetMemberAsync(guildId, requesterId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);

        var m1 = new GuildMember { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Active, ContributionPoints = 10, JoinedAt = DateTimeOffset.UtcNow.AddDays(-5) };
        var m2 = new GuildMember { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Active, ContributionPoints = 10, JoinedAt = DateTimeOffset.UtcNow.AddDays(-3) };
        var m3 = new GuildMember { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Active, ContributionPoints = 50, JoinedAt = DateTimeOffset.UtcNow.AddDays(-2) };
        var m4 = new GuildMember { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Inactive, ContributionPoints = 100 };
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new[] { m1, m2, m3, m4 });

        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo);
        IEnumerable<GuildMember>? captured = null;
        memberRepo.UpdateRangeAsync(Arg.Any<IEnumerable<GuildMember>>(), Arg.Any<CancellationToken>())
                  .Returns(ci => {
                      captured = ci.ArgAt<IEnumerable<GuildMember>>(0);
                      return Task.FromResult(captured);
                  });

        await sut.Handle(new ApproveGuildJoinRequestCommand(guildId, reqId, Guid.NewGuid()), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Single(x => x.AuthUserId == m3.AuthUserId).RankWithinGuild.Should().Be(1);
        captured!.Single(x => x.AuthUserId == m1.AuthUserId).RankWithinGuild.Should().Be(2);
        captured!.Single(x => x.AuthUserId == m2.AuthUserId).RankWithinGuild.Should().Be(3);
        captured!.Single(x => x.AuthUserId == m4.AuthUserId).RankWithinGuild.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MismatchedGuildOnRequest_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();

        var guildId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 100 });
        joinRepo.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(new GuildJoinRequest { Id = reqId, GuildId = Guid.NewGuid(), Status = GuildJoinRequestStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });

        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo);
        var act = () => sut.Handle(new ApproveGuildJoinRequestCommand(guildId, reqId, Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_ExpiredOrNonPendingRequest_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();

        var guildId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 100 });
        joinRepo.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(new GuildJoinRequest { Id = reqId, GuildId = guildId, Status = GuildJoinRequestStatus.Declined, ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) });

        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo);
        var act = () => sut.Handle(new ApproveGuildJoinRequestCommand(guildId, reqId, Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_RequesterInOtherGuild_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();

        var guildId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 100 });
        joinRepo.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(new GuildJoinRequest { Id = reqId, GuildId = guildId, RequesterId = requesterId, Status = GuildJoinRequestStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });
        memberRepo.GetMembershipsByUserAsync(requesterId, Arg.Any<CancellationToken>()).Returns(new[] { new GuildMember { GuildId = Guid.NewGuid(), AuthUserId = requesterId, Status = MemberStatus.Active } });

        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo);
        var act = () => sut.Handle(new ApproveGuildJoinRequestCommand(guildId, reqId, Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_CapacityFull_ThrowsBadRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();

        var guildId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 1 });
        joinRepo.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(new GuildJoinRequest { Id = reqId, GuildId = guildId, RequesterId = requesterId, Status = GuildJoinRequestStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(1);

        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo);
        var act = () => sut.Handle(new ApproveGuildJoinRequestCommand(guildId, reqId, Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_Success_AddsMember_UpdatesCounts_CleansRequestsAndInvites()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();

        var guildId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        var guild = new Guild { Id = guildId, MaxMembers = 100, CurrentMemberCount = 0 };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        var joinReq = new GuildJoinRequest { Id = reqId, GuildId = guildId, RequesterId = requesterId, Status = GuildJoinRequestStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        joinRepo.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(joinReq);

        memberRepo.GetMembershipsByUserAsync(requesterId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.CountActiveMembersAsync(guildId, Arg.Any<CancellationToken>()).Returns(0, 1);
        memberRepo.GetMemberAsync(guildId, requesterId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new() { AuthUserId = requesterId, Status = MemberStatus.Active, ContributionPoints = 0, JoinedAt = DateTimeOffset.UtcNow } });

        joinRepo.GetRequestsByRequesterAsync(requesterId, Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequest> {
            new() { Id = Guid.NewGuid(), GuildId = Guid.NewGuid(), RequesterId = requesterId, Status = GuildJoinRequestStatus.Pending }
        });

        inviteRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GuildInvitation, bool>>>(), Arg.Any<CancellationToken>())
                  .Returns(new List<GuildInvitation> { new() { Id = Guid.NewGuid(), InviteeId = requesterId, Status = InvitationStatus.Pending, GuildId = Guid.NewGuid() } });

        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo);
        await sut.Handle(new ApproveGuildJoinRequestCommand(guildId, reqId, Guid.NewGuid()), CancellationToken.None);

        await memberRepo.Received(1).AddAsync(Arg.Any<GuildMember>(), Arg.Any<CancellationToken>());
        await guildRepo.Received(1).UpdateAsync(Arg.Is<Guild>(g => g.CurrentMemberCount == 1), Arg.Any<CancellationToken>());
        await joinRepo.Received(1).UpdateAsync(Arg.Is<GuildJoinRequest>(r => r.Status == GuildJoinRequestStatus.Accepted), Arg.Any<CancellationToken>());
        await joinRepo.Received(1).DeleteRangeAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
        await inviteRepo.Received(1).UpdateRangeAsync(Arg.Is<IEnumerable<GuildInvitation>>(l => l.All(i => i.Status == InvitationStatus.Declined)), Arg.Any<CancellationToken>());
        await notify.Received(1).NotifyJoinRequestApprovedAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingMember_DoesNotAddAgain_StillAccepts()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var joinRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var inviteRepo = Substitute.For<IGuildInvitationRepository>();

        var guildId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, MaxMembers = 100 });
        joinRepo.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(new GuildJoinRequest { Id = reqId, GuildId = guildId, RequesterId = requesterId, Status = GuildJoinRequestStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });

        memberRepo.GetMembershipsByUserAsync(requesterId, Arg.Any<CancellationToken>()).Returns(Array.Empty<GuildMember>());
        memberRepo.GetMemberAsync(guildId, requesterId, Arg.Any<CancellationToken>()).Returns(new GuildMember { GuildId = guildId, AuthUserId = requesterId, Status = MemberStatus.Active });

        var sut = new ApproveGuildJoinRequestCommandHandler(guildRepo, memberRepo, joinRepo, notify, inviteRepo);
        await sut.Handle(new ApproveGuildJoinRequestCommand(guildId, reqId, Guid.NewGuid()), CancellationToken.None);

        await memberRepo.DidNotReceive().AddAsync(Arg.Any<GuildMember>(), Arg.Any<CancellationToken>());
        await memberRepo.DidNotReceive().UpdateRangeAsync(Arg.Any<IEnumerable<GuildMember>>(), Arg.Any<CancellationToken>());
        await guildRepo.DidNotReceive().UpdateAsync(Arg.Any<Guild>(), Arg.Any<CancellationToken>());
        await joinRepo.Received(1).UpdateAsync(Arg.Is<GuildJoinRequest>(r => r.Status == GuildJoinRequestStatus.Accepted), Arg.Any<CancellationToken>());
    }
}
