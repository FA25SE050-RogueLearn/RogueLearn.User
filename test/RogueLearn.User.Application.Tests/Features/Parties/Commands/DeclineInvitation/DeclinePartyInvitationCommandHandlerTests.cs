using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.DeclineInvitation;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.DeclineInvitation;

public class DeclinePartyInvitationCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_Success_UpdatesInvitation(DeclinePartyInvitationCommand cmd)
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var sut = new DeclinePartyInvitationCommandHandler(invitationRepo);

        var inv = new PartyInvitation { Id = cmd.InvitationId, PartyId = cmd.PartyId, InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1) };
        invitationRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);

        await sut.Handle(cmd, CancellationToken.None);
        await invitationRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.Status == InvitationStatus.Declined), Arg.Any<CancellationToken>());
    }
}