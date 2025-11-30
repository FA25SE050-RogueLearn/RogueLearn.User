using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RogueLearn.User.Application.Features.Guilds.Commands.ApproveJoinRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ApproveJoinRequest;

public class ApproveGuildJoinRequestCommandHandlerTests
{
    [Fact]
    public async Task ApproveJoinRequest_RecalculatesRanks_WhenNewMemberJoins()
    {
        var guildRepo = new Mock<IGuildRepository>(MockBehavior.Strict);
        var memberRepo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var joinReqRepo = new Mock<IGuildJoinRequestRepository>(MockBehavior.Strict);

        var guildId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        guildRepo.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guild { Id = guildId, MaxMembers = 50, CurrentMemberCount = 2 });

        joinReqRepo.Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildJoinRequest
            {
                Id = requestId,
                GuildId = guildId,
                RequesterId = requesterId,
                Status = GuildJoinRequestStatus.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
            });

        memberRepo.Setup(r => r.GetMembershipsByUserAsync(requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GuildMember>());
        guildRepo.Setup(r => r.GetGuildsByCreatorAsync(requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guild>());

        memberRepo.Setup(r => r.CountActiveMembersAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        memberRepo.Setup(r => r.GetMemberAsync(guildId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildMember?)null);

        memberRepo.Setup(r => r.AddAsync(It.IsAny<GuildMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildMember m, CancellationToken _) => m);

        guildRepo.Setup(r => r.UpdateAsync(It.IsAny<Guild>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guild g, CancellationToken _) => g);

        var existingMembers = new[]
        {
            new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Active, ContributionPoints = 100, JoinedAt = DateTimeOffset.UtcNow.AddDays(-10) },
            new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Active, ContributionPoints = 50, JoinedAt = DateTimeOffset.UtcNow.AddDays(-5) },
            new GuildMember { GuildId = guildId, AuthUserId = requesterId, Status = MemberStatus.Active, ContributionPoints = 0, JoinedAt = DateTimeOffset.UtcNow }
        };

        memberRepo.Setup(r => r.GetMembersByGuildAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMembers);

        IEnumerable<GuildMember> updatedRange = Enumerable.Empty<GuildMember>();
        memberRepo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<GuildMember>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<GuildMember> ms, CancellationToken _) =>
            {
                updatedRange = ms.ToList();
                return updatedRange;
            });

        joinReqRepo.Setup(r => r.GetRequestsByRequesterAsync(requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GuildJoinRequest>());

        joinReqRepo.Setup(r => r.UpdateAsync(It.IsAny<GuildJoinRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildJoinRequest jr, CancellationToken _) => jr);

        var handler = new ApproveGuildJoinRequestCommandHandler(guildRepo.Object, memberRepo.Object, joinReqRepo.Object);
        await handler.Handle(new ApproveGuildJoinRequestCommand(guildId, requestId, Guid.NewGuid()), CancellationToken.None);

        var active = updatedRange.Where(m => m.Status == MemberStatus.Active).OrderByDescending(m => m.ContributionPoints).ThenBy(m => m.JoinedAt).ToList();
        Assert.Equal(1, active[0].RankWithinGuild);
        Assert.Equal(2, active[1].RankWithinGuild);
        Assert.Equal(3, active[2].RankWithinGuild);
    }
}