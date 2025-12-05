using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.DeclineInvitation;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.DeclineInvitation;

public class DeclinePartyInvitationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_UpdatesInvitation()
    {
        var cmd = new DeclinePartyInvitationCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid());
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<IPartyNotificationService>();
        var sut = new DeclinePartyInvitationCommandHandler(invitationRepo, notification);

        var inv = new PartyInvitation { Id = cmd.InvitationId, PartyId = cmd.PartyId, InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1) };
        invitationRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);

        await sut.Handle(cmd, CancellationToken.None);
        await invitationRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.Status == InvitationStatus.Declined), Arg.Any<CancellationToken>());
    }
}