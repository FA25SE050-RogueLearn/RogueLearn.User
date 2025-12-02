using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.AcceptInvitation;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.AcceptInvitation;

public class AcceptPartyInvitationCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_Success_AddsMemberAndUpdatesInvitation(AcceptPartyInvitationCommand cmd)
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var partyRepo = Substitute.For<IPartyRepository>();
        var sut = new AcceptPartyInvitationCommandHandler(invitationRepo, memberRepo, partyRepo);

        var inv = new PartyInvitation { Id = cmd.InvitationId, PartyId = cmd.PartyId, InviteeId = cmd.AuthUserId, Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1) };
        invitationRepo.GetByIdAsync(cmd.InvitationId, Arg.Any<CancellationToken>()).Returns(inv);
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns((PartyMember?)null);

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received(1).AddAsync(Arg.Is<PartyMember>(m => m.PartyId == cmd.PartyId && m.AuthUserId == cmd.AuthUserId && m.Role == PartyRole.Member), Arg.Any<CancellationToken>());
        await invitationRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.Status == InvitationStatus.Accepted), Arg.Any<CancellationToken>());
    }
}