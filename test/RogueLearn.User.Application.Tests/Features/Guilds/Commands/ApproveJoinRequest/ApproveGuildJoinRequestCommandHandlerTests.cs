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
}