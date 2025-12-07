using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.AcceptInvitation;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.AcceptInvitation;

public class AcceptPartyInvitationCommandHandlerTests
{
    [Fact]
    public async Task Handle_PartyMismatch_ThrowsBadRequest()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new AcceptPartyInvitationCommandHandler(invRepo, memberRepo, notify);

        var invitationId = System.Guid.NewGuid();
        var partyId = System.Guid.NewGuid();
        invRepo.GetByIdAsync(invitationId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = invitationId, PartyId = System.Guid.NewGuid(), Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1), InviteeId = System.Guid.NewGuid() });

        var cmd = new AcceptPartyInvitationCommand(partyId, invitationId, System.Guid.NewGuid());
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ActivatesMemberAndAccepts()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new AcceptPartyInvitationCommandHandler(invRepo, memberRepo, notify);

        var invitationId = System.Guid.NewGuid();
        var partyId = System.Guid.NewGuid();
        var userId = System.Guid.NewGuid();
        var inv = new PartyInvitation { Id = invitationId, PartyId = partyId, Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1), InviteeId = userId };
        invRepo.GetByIdAsync(invitationId, Arg.Any<CancellationToken>()).Returns(inv);
        memberRepo.GetMemberAsync(partyId, userId, Arg.Any<CancellationToken>()).Returns((PartyMember?)null);

        var cmd = new AcceptPartyInvitationCommand(partyId, invitationId, userId);
        await sut.Handle(cmd, CancellationToken.None);
        await invRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.Status == InvitationStatus.Accepted), Arg.Any<CancellationToken>());
        await memberRepo.Received(1).AddAsync(Arg.Is<PartyMember>(m => m.PartyId == partyId && m.AuthUserId == userId && m.Status == MemberStatus.Active), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotIntendedUser_ThrowsForbidden()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new AcceptPartyInvitationCommandHandler(invRepo, memberRepo, notify);

        var invitationId = System.Guid.NewGuid();
        var partyId = System.Guid.NewGuid();
        var userId = System.Guid.NewGuid();
        invRepo.GetByIdAsync(invitationId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = invitationId, PartyId = partyId, Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1), InviteeId = System.Guid.NewGuid() });

        var cmd = new AcceptPartyInvitationCommand(partyId, invitationId, userId);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExpiredInvitation_ThrowsBadRequest()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new AcceptPartyInvitationCommandHandler(invRepo, memberRepo, notify);

        var invitationId = System.Guid.NewGuid();
        var partyId = System.Guid.NewGuid();
        var userId = System.Guid.NewGuid();
        invRepo.GetByIdAsync(invitationId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = invitationId, PartyId = partyId, Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddSeconds(-1), InviteeId = userId });

        var cmd = new AcceptPartyInvitationCommand(partyId, invitationId, userId);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyActiveMember_ThrowsBadRequest()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new AcceptPartyInvitationCommandHandler(invRepo, memberRepo, notify);

        var invitationId = System.Guid.NewGuid();
        var partyId = System.Guid.NewGuid();
        var userId = System.Guid.NewGuid();
        invRepo.GetByIdAsync(invitationId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = invitationId, PartyId = partyId, Status = InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1), InviteeId = userId });
        memberRepo.GetMemberAsync(partyId, userId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = partyId, AuthUserId = userId, Status = MemberStatus.Active });

        var cmd = new AcceptPartyInvitationCommand(partyId, invitationId, userId);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
