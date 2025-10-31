using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.ManageRoles;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.RevokeGuildRole;

public class RevokeGuildRoleCommandHandlerTests
{
    [Fact]
    public async Task RevokeGuildRole_SetsBaselineMember()
    {
        var repo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsGuildMasterAsync(guildId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(guildId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { GuildId = guildId, AuthUserId = memberId, Role = GuildRole.Officer });
        repo.Setup(r => r.UpdateAsync(It.IsAny<GuildMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildMember m, CancellationToken _) => m);

        var handler = new RevokeGuildRoleCommandHandler(repo.Object);
        await handler.Handle(new RevokeGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId), CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(It.Is<GuildMember>(m => m.Role == GuildRole.Member), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeGuildRole_Throws_WhenRevokingBaseline()
    {
        var repo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsGuildMasterAsync(guildId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(guildId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { GuildId = guildId, AuthUserId = memberId, Role = GuildRole.Member });

        var handler = new RevokeGuildRoleCommandHandler(repo.Object);
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(new RevokeGuildRoleCommand(guildId, memberId, GuildRole.Member, actorId), CancellationToken.None));
    }

    [Fact]
    public async Task RevokeGuildRole_NoOp_WhenMemberDoesNotHaveRole()
    {
        var repo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsGuildMasterAsync(guildId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(guildId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { GuildId = guildId, AuthUserId = memberId, Role = GuildRole.Member });

        var handler = new RevokeGuildRoleCommandHandler(repo.Object);
        await handler.Handle(new RevokeGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId), CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(It.IsAny<GuildMember>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}