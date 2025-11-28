using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RogueLearn.User.Application.Features.Guilds.Commands.RemoveMember;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.RemoveMember;

public class RemoveGuildMemberCommandHandlerTests
{
    [Fact]
    public async Task RemoveMember_RecalculatesRanks_AfterMemberRemoval()
    {
        var memberRepo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildRepo = new Mock<IGuildRepository>(MockBehavior.Strict);

        var guildId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        memberRepo.Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { Id = memberId, GuildId = guildId, AuthUserId = Guid.NewGuid(), Role = GuildRole.Member, Status = MemberStatus.Active });

        memberRepo.Setup(r => r.DeleteAsync(memberId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        memberRepo.Setup(r => r.CountActiveMembersAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        guildRepo.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guild { Id = guildId, MaxMembers = 50, CurrentMemberCount = 3 });

        guildRepo.Setup(r => r.UpdateAsync(It.IsAny<Guild>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guild g, CancellationToken _) => g);

        var remaining = new[]
        {
            new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Active, ContributionPoints = 200, JoinedAt = DateTimeOffset.UtcNow.AddDays(-10) },
            new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Active, ContributionPoints = 100, JoinedAt = DateTimeOffset.UtcNow.AddDays(-8) },
        };

        memberRepo.Setup(r => r.GetMembersByGuildAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remaining);

        IEnumerable<GuildMember> updatedRange = Enumerable.Empty<GuildMember>();
        memberRepo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<GuildMember>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<GuildMember> ms, CancellationToken _) =>
            {
                updatedRange = ms.ToList();
                return updatedRange;
            });

        var handler = new RemoveGuildMemberCommandHandler(memberRepo.Object, guildRepo.Object);
        await handler.Handle(new RemoveGuildMemberCommand(guildId, memberId, null), CancellationToken.None);

        var active = updatedRange.OrderByDescending(m => m.ContributionPoints).ThenBy(m => m.JoinedAt).ToList();
        Assert.Equal(1, active[0].RankWithinGuild);
        Assert.Equal(2, active[1].RankWithinGuild);
    }
}