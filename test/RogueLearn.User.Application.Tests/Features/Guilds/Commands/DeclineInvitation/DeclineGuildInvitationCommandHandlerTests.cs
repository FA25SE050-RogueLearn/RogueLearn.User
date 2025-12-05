using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.DeclineInvitation;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.DeclineInvitation;

public class DeclineGuildInvitationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_DeclinesInvitation()
    {
        var invRepo = Substitute.For<IGuildInvitationRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var sut = new DeclineGuildInvitationCommandHandler(invRepo, memberRepo, notificationService);
        var cmd = new DeclineGuildInvitationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var inv = new GuildInvitation { Id = cmd.InvitationId, GuildId = cmd.GuildId, InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1) };
        invRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);
        await sut.Handle(cmd, CancellationToken.None);
        await invRepo.Received(1).UpdateAsync(Arg.Is<GuildInvitation>(i => i.Status == InvitationStatus.Declined), Arg.Any<CancellationToken>());
    }
}