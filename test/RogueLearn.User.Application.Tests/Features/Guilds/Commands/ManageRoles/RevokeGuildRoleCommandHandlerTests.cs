using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.ManageRoles;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ManageRoles;

public class RevokeGuildRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotMaster_Throws()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new RevokeGuildRoleCommandHandler(repo);
        var guildId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new RevokeGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId, false);
        repo.IsGuildMasterAsync(guildId, actorId, Arg.Any<CancellationToken>()).Returns(false);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AdminOverride_RevokesToMember()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new RevokeGuildRoleCommandHandler(repo);
        var guildId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new RevokeGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId, true);
        var member = new RogueLearn.User.Domain.Entities.GuildMember { GuildId = guildId, AuthUserId = memberId, Role = GuildRole.Officer };
        repo.GetMemberAsync(guildId, memberId, Arg.Any<CancellationToken>()).Returns(member);

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<RogueLearn.User.Domain.Entities.GuildMember>(m => m.Role == GuildRole.Member), Arg.Any<CancellationToken>());
    }
}