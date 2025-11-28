using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RogueLearn.User.Application.Features.Guilds.Commands.LeaveGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.LeaveGuild;

public class LeaveGuildCommandHandlerTests
{
    [Fact]
    public async Task LeaveGuild_RecalculatesRanks_AfterMemberLeaves()
    {
        var memberRepo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildRepo = new Mock<IGuildRepository>(MockBehavior.Strict);

        var guildId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        memberRepo.Setup(r => r.GetMemberAsync(guildId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { Id = Guid.NewGuid(), GuildId = guildId, AuthUserId = userId, Role = GuildRole.Member, Status = MemberStatus.Active });

        memberRepo.Setup(r => r.CountActiveMembersAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        memberRepo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        guildRepo.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guild { Id = guildId, MaxMembers = 50, CurrentMemberCount = 3 });

        guildRepo.Setup(r => r.UpdateAsync(It.IsAny<Guild>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guild g, CancellationToken _) => g);

        var remaining = new[]
        {
            new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Active, ContributionPoints = 120, JoinedAt = DateTimeOffset.UtcNow.AddDays(-5) },
            new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Active, ContributionPoints = 80, JoinedAt = DateTimeOffset.UtcNow.AddDays(-3) },
            new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Status = MemberStatus.Inactive, ContributionPoints = 999, JoinedAt = DateTimeOffset.UtcNow.AddDays(-1) }
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

        var handler = new LeaveGuildCommandHandler(memberRepo.Object, guildRepo.Object);
        await handler.Handle(new LeaveGuildCommand(guildId, userId), CancellationToken.None);

        var active = updatedRange.Where(m => m.Status == MemberStatus.Active).OrderByDescending(m => m.ContributionPoints).ThenBy(m => m.JoinedAt).ToList();
        Assert.Equal(1, active[0].RankWithinGuild);
        Assert.Equal(2, active[1].RankWithinGuild);
        var inactive = updatedRange.First(m => m.Status != MemberStatus.Active);
        Assert.Null(inactive.RankWithinGuild);
    }
}