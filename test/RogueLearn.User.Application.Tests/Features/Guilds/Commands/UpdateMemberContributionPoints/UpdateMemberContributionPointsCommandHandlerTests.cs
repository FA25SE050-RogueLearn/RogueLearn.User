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
}