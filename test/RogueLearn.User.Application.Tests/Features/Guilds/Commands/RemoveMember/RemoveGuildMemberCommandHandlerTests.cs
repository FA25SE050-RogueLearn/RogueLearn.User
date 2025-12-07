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
    public async Task Handle_MemberBelongsToDifferentGuild_ThrowsBadRequest()
    {
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new RemoveGuildMemberCommandHandler(memberRepo, guildRepo, notificationService);

        var cmd = new RemoveGuildMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "reason");
        var member = new GuildMember { Id = cmd.MemberId, GuildId = Guid.NewGuid(), Role = GuildRole.Member };
        memberRepo.GetByIdAsync(cmd.MemberId, Arg.Any<CancellationToken>()).Returns(member);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RankingUpdates_ForActiveAndNonActive()
    {
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new RemoveGuildMemberCommandHandler(memberRepo, guildRepo, notificationService);

        var guildId = Guid.NewGuid();
        var cmd = new RemoveGuildMemberCommand(guildId, Guid.NewGuid(), "reason");
        var member = new GuildMember { Id = cmd.MemberId, GuildId = guildId, Role = GuildRole.Member };
        memberRepo.GetByIdAsync(cmd.MemberId, Arg.Any<CancellationToken>()).Returns(member);
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, CurrentMemberCount = 3 });

        var members = new List<GuildMember>
        {
            new() { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Active, ContributionPoints = 10, JoinedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Active, ContributionPoints = 20, JoinedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { AuthUserId = Guid.NewGuid(), GuildId = guildId, Status = MemberStatus.Left, ContributionPoints = 0 }
        };
        memberRepo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>()).Returns(members);

        await sut.Handle(cmd, CancellationToken.None);

        await memberRepo.Received(1).UpdateRangeAsync(Arg.Is<IEnumerable<GuildMember>>(list =>
            list.Count() == 3 &&
            list.Where(m => m.Status == MemberStatus.Active).OrderByDescending(m => m.ContributionPoints).First().RankWithinGuild == 1 &&
            list.Any(m => m.Status != MemberStatus.Active && m.RankWithinGuild == null)
        ), Arg.Any<CancellationToken>());
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
