using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.ManageRoles;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ManageRoles;

public class AssignGuildRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotMaster_Throws()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new AssignGuildRoleCommandHandler(repo);
        var guildId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new AssignGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId, false);
        repo.IsGuildMasterAsync(guildId, actorId, Arg.Any<CancellationToken>()).Returns(false);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AdminOverride_AssignsRole()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new AssignGuildRoleCommandHandler(repo);
        var guildId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new AssignGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId, true);
        var member = new RogueLearn.User.Domain.Entities.GuildMember { GuildId = guildId, AuthUserId = memberId, Role = GuildRole.Member };
        repo.GetMemberAsync(guildId, memberId, Arg.Any<CancellationToken>()).Returns(member);

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<RogueLearn.User.Domain.Entities.GuildMember>(m => m.Role == GuildRole.Officer), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotFoundMember_Throws()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new AssignGuildRoleCommandHandler(repo);
        var guildId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new AssignGuildRoleCommand(guildId, memberId, GuildRole.Officer, actorId, true);
        repo.GetMemberAsync(guildId, memberId, Arg.Any<CancellationToken>()).Returns((RogueLearn.User.Domain.Entities.GuildMember?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AssignGuildMaster_ThrowsUnprocessable()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new AssignGuildRoleCommandHandler(repo);
        var guildId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new AssignGuildRoleCommand(guildId, memberId, GuildRole.GuildMaster, actorId, true);
        repo.GetMemberAsync(guildId, memberId, Arg.Any<CancellationToken>()).Returns(new RogueLearn.User.Domain.Entities.GuildMember { GuildId = guildId, AuthUserId = memberId, Role = GuildRole.Member });
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
