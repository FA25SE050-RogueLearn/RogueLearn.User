using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.UpdateMemberContributionPoints;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.UpdateMemberContributionPoints;

public class UpdateMemberContributionPointsCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var cmd = new UpdateMemberContributionPointsCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), 1);
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var sut = new UpdateMemberContributionPointsCommandHandler(memberRepo);
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.MemberAuthUserId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RankingActiveAndNonActive_AssignsRanks()
    {
        var guildId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        var cmd = new UpdateMemberContributionPointsCommand(guildId, memberId, 2);
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var sut = new UpdateMemberContributionPointsCommandHandler(memberRepo);

        var target = new GuildMember { GuildId = guildId, AuthUserId = memberId, ContributionPoints = 5, Status = MemberStatus.Active, JoinedAt = System.DateTimeOffset.UtcNow.AddDays(-3) };
        var otherActive = new GuildMember { GuildId = guildId, AuthUserId = System.Guid.NewGuid(), ContributionPoints = 10, Status = MemberStatus.Active, JoinedAt = System.DateTimeOffset.UtcNow.AddDays(-2) };
        var nonActive = new GuildMember { GuildId = guildId, AuthUserId = System.Guid.NewGuid(), ContributionPoints = 8, Status = MemberStatus.Left, JoinedAt = System.DateTimeOffset.UtcNow.AddDays(-1) };

        memberRepo.GetMemberAsync(guildId, memberId, Arg.Any<CancellationToken>()).Returns(target);
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { target, otherActive, nonActive });

        await sut.Handle(cmd, CancellationToken.None);

        await memberRepo.Received(1).UpdateRangeAsync(Arg.Is<IEnumerable<GuildMember>>(list =>
            list.Any(m => m.AuthUserId == otherActive.AuthUserId && m.RankWithinGuild == 1) &&
            list.Any(m => m.AuthUserId == target.AuthUserId && m.RankWithinGuild == 2) &&
            list.Any(m => m.AuthUserId == nonActive.AuthUserId && m.RankWithinGuild == null)
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_UpdatesAndRanks()
    {
        var cmd = new UpdateMemberContributionPointsCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), 5);
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var sut = new UpdateMemberContributionPointsCommandHandler(memberRepo);
        var member = new GuildMember { GuildId = cmd.GuildId, AuthUserId = cmd.MemberAuthUserId, ContributionPoints = 10 };
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.MemberAuthUserId, Arg.Any<CancellationToken>()).Returns(member);
        memberRepo.GetMembersByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { member });

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received(1).UpdateAsync(Arg.Any<GuildMember>(), Arg.Any<CancellationToken>());
        await memberRepo.Received(1).UpdateRangeAsync(Arg.Any<IEnumerable<GuildMember>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeDelta_AllowsDecrement()
    {
        var cmd = new UpdateMemberContributionPointsCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), -3);
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var sut = new UpdateMemberContributionPointsCommandHandler(memberRepo);
        var member = new GuildMember { GuildId = cmd.GuildId, AuthUserId = cmd.MemberAuthUserId, ContributionPoints = 10, Status = MemberStatus.Active };
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.MemberAuthUserId, Arg.Any<CancellationToken>()).Returns(member);
        memberRepo.GetMembersByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { member });

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received(1).UpdateAsync(Arg.Is<GuildMember>(m => m.ContributionPoints == 7), Arg.Any<CancellationToken>());
    }
}
