using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.InviteMember;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.InviteMember;

public class InviteMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_EmailResolvesAndAdds()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var invitee = new UserProfile { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Email = "target@example.com" };
        var target = new InviteTarget(null, invitee.Email);
        var cmd = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { target }, "msg", DateTimeOffset.UtcNow.AddDays(2));

        userRepo.GetByEmailAsync(invitee.Email, Arg.Any<CancellationToken>()).Returns(invitee);
        invRepo.GetByPartyAndInviteeAsync(cmd.PartyId, invitee.AuthUserId, Arg.Any<CancellationToken>()).Returns((PartyInvitation?)null);
        invRepo.AddAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyInvitation>());

        await sut.Handle(cmd, CancellationToken.None);
        await invRepo.Received(1).AddAsync(Arg.Is<PartyInvitation>(i => i.PartyId == cmd.PartyId && i.InviterId == cmd.InviterAuthUserId && i.Status == InvitationStatus.Pending), Arg.Any<CancellationToken>());
        await notifService.Received(1).SendInvitationNotificationAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>());
    }
}