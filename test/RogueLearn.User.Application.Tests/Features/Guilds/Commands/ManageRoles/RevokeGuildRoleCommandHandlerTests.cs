using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.ManageRoles;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ManageRoles;

public class RevokeGuildRoleCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotMaster_Throws(RevokeGuildRoleCommand cmd)
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new RevokeGuildRoleCommandHandler(repo);
        repo.IsGuildMasterAsync(cmd.GuildId, cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(false);
        cmd = new RevokeGuildRoleCommand(cmd.GuildId, cmd.MemberAuthUserId, GuildRole.Officer, cmd.ActorAuthUserId, false);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_AdminOverride_RevokesToMember(RevokeGuildRoleCommand cmd)
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var sut = new RevokeGuildRoleCommandHandler(repo);
        var member = new RogueLearn.User.Domain.Entities.GuildMember { GuildId = cmd.GuildId, AuthUserId = cmd.MemberAuthUserId, Role = GuildRole.Officer };
        repo.GetMemberAsync(cmd.GuildId, cmd.MemberAuthUserId, Arg.Any<CancellationToken>()).Returns(member);

        cmd = new RevokeGuildRoleCommand(cmd.GuildId, cmd.MemberAuthUserId, GuildRole.Officer, cmd.ActorAuthUserId, true);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<RogueLearn.User.Domain.Entities.GuildMember>(m => m.Role == GuildRole.Member), Arg.Any<CancellationToken>());
    }
}