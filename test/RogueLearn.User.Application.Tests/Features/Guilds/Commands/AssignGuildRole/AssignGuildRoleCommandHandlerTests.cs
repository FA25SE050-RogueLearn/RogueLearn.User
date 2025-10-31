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

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.AssignGuildRole;

public class AssignGuildRoleCommandHandlerTests
{
    [Fact]
    public async Task AssignGuildRole_Succeeds_ForGuildMaster()
    {
        var repo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsGuildMasterAsync(guildId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(guildId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { GuildId = guildId, AuthUserId = memberId, Role = GuildRole.Member });
        repo.Setup(r => r.UpdateAsync(It.IsAny<GuildMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildMember m, CancellationToken _) => m);

        var handler = new AssignGuildRoleCommandHandler(repo.Object);
        await handler.Handle(new AssignGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId), CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(It.Is<GuildMember>(m => m.Role == GuildRole.Officer), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignGuildRole_Throws_WhenActorNotMaster()
    {
        var repo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsGuildMasterAsync(guildId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new AssignGuildRoleCommandHandler(repo.Object);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new AssignGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId), CancellationToken.None));
    }

    [Fact]
    public async Task AssignGuildRole_Throws_WhenAssigningGuildMaster()
    {
        var repo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsGuildMasterAsync(guildId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(guildId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { GuildId = guildId, AuthUserId = memberId, Role = GuildRole.Member });

        var handler = new AssignGuildRoleCommandHandler(repo.Object);
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(new AssignGuildRoleCommand(guildId, memberId, GuildRole.GuildMaster, actorId), CancellationToken.None));
    }

    [Fact]
    public async Task AssignGuildRole_NoOp_WhenRoleAlreadyAssigned()
    {
        var repo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var guildId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        repo.Setup(r => r.IsGuildMasterAsync(guildId, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetMemberAsync(guildId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { GuildId = guildId, AuthUserId = memberId, Role = GuildRole.Officer });

        var handler = new AssignGuildRoleCommandHandler(repo.Object);
        await handler.Handle(new AssignGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId), CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(It.IsAny<GuildMember>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}