using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.DeclineInvitation;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.DeclineInvitation;

public class DeclineGuildInvitationCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_Success_DeclinesInvitation(DeclineGuildInvitationCommand cmd)
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var sut = new DeclineGuildInvitationCommandHandler(invRepo);
        var inv = new GuildInvitation { Id = cmd.InvitationId, GuildId = cmd.GuildId, InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1) };
        invRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);
        await sut.Handle(cmd, CancellationToken.None);
        await invRepo.Received(1).UpdateAsync(Arg.Is<GuildInvitation>(i => i.Status == InvitationStatus.Declined), Arg.Any<CancellationToken>());
    }
}