using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.RemoveMember;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.RemoveMember;

public class RemoveGuildMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_CannotRemoveMaster()
    {
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new RemoveGuildMemberCommandHandler(memberRepo, guildRepo, notificationService);

        var cmd = new RemoveGuildMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "reason");
        var member = new GuildMember { Id = cmd.MemberId, GuildId = cmd.GuildId, Role = GuildRole.GuildMaster };
        memberRepo.GetByIdAsync(cmd.MemberId, Arg.Any<CancellationToken>()).Returns(member);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_DeletesAndUpdates()
    {
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new RemoveGuildMemberCommandHandler(memberRepo, guildRepo, notificationService);

        var cmd = new RemoveGuildMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "reason");
        var member = new GuildMember { Id = cmd.MemberId, GuildId = cmd.GuildId, Role = GuildRole.Member };
        memberRepo.GetByIdAsync(cmd.MemberId, Arg.Any<CancellationToken>()).Returns(member);
        var guild = new Guild { Id = cmd.GuildId, CurrentMemberCount = 10 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMembersByGuildAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received(1).DeleteAsync(member.Id, Arg.Any<CancellationToken>());
        await guildRepo.Received(1).UpdateAsync(Arg.Any<Guild>(), Arg.Any<CancellationToken>());
        await memberRepo.Received(1).UpdateRangeAsync(Arg.Any<IEnumerable<GuildMember>>(), Arg.Any<CancellationToken>());
    }
}