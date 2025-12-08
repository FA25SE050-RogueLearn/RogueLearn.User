using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.AcceptInvitation;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.AcceptInvitation;

public class AcceptPartyInvitationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingMember_Active_ThrowsBadRequest()
    {
        var inviteRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notifier = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();

        var partyId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        inviteRepo.GetByIdAsync(inviteId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = inviteId, PartyId = partyId, InviteeId = authId, Status = InvitationStatus.Pending });
        memberRepo.GetMemberAsync(partyId, authId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = partyId, AuthUserId = authId, Status = MemberStatus.Active });

        var sut = new AcceptPartyInvitationCommandHandler(inviteRepo, memberRepo, notifier);
        var act = () => sut.Handle(new AcceptPartyInvitationCommand(partyId, inviteId, authId), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_ExistingMember_NotActive_Reactivates()
    {
        var inviteRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notifier = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();

        var partyId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        inviteRepo.GetByIdAsync(inviteId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = inviteId, PartyId = partyId, InviteeId = authId, Status = InvitationStatus.Pending });
        var existing = new PartyMember { PartyId = partyId, AuthUserId = authId, Status = MemberStatus.Inactive };
        memberRepo.GetMemberAsync(partyId, authId, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new AcceptPartyInvitationCommandHandler(inviteRepo, memberRepo, notifier);
        PartyMember? updated = null;
        memberRepo.UpdateAsync(Arg.Do<PartyMember>(m => updated = m), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyMember>());

        await sut.Handle(new AcceptPartyInvitationCommand(partyId, inviteId, authId), CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(MemberStatus.Active);
        updated.LeftAt.Should().BeNull();
        updated.JoinedAt.Should().NotBe(default);
        await inviteRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.Status == InvitationStatus.Accepted), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvitationNotFound_ThrowsNotFound()
    {
        var inviteRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notifier = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();

        var partyId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        inviteRepo.GetByIdAsync(inviteId, Arg.Any<CancellationToken>()).Returns((PartyInvitation?)null);

        var sut = new AcceptPartyInvitationCommandHandler(inviteRepo, memberRepo, notifier);
        var act = () => sut.Handle(new AcceptPartyInvitationCommand(partyId, inviteId, authId), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_PartyIdMismatch_ThrowsBadRequest()
    {
        var inviteRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notifier = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();

        var partyId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        inviteRepo.GetByIdAsync(inviteId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = inviteId, PartyId = Guid.NewGuid(), InviteeId = authId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });

        var sut = new AcceptPartyInvitationCommandHandler(inviteRepo, memberRepo, notifier);
        var act = () => sut.Handle(new AcceptPartyInvitationCommand(partyId, inviteId, authId), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_InvitationExpired_ThrowsBadRequest()
    {
        var inviteRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notifier = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();

        var partyId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        inviteRepo.GetByIdAsync(inviteId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = inviteId, PartyId = partyId, InviteeId = authId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) });

        var sut = new AcceptPartyInvitationCommandHandler(inviteRepo, memberRepo, notifier);
        var act = () => sut.Handle(new AcceptPartyInvitationCommand(partyId, inviteId, authId), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_InviteeMismatch_ThrowsForbidden()
    {
        var inviteRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notifier = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();

        var partyId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        inviteRepo.GetByIdAsync(inviteId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = inviteId, PartyId = partyId, InviteeId = Guid.NewGuid(), Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });

        var sut = new AcceptPartyInvitationCommandHandler(inviteRepo, memberRepo, notifier);
        var act = () => sut.Handle(new AcceptPartyInvitationCommand(partyId, inviteId, authId), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.ForbiddenException>();
    }

    [Fact]
    public async Task Handle_NoExistingMember_AddsAndSendsNotification()
    {
        var inviteRepo = Substitute.For<IPartyInvitationRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var notifier = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();

        var partyId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        inviteRepo.GetByIdAsync(inviteId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Id = inviteId, PartyId = partyId, InviteeId = authId, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });
        memberRepo.GetMemberAsync(partyId, authId, Arg.Any<CancellationToken>()).Returns((PartyMember?)null);
        memberRepo.AddAsync(Arg.Any<PartyMember>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyMember>());

        var sut = new AcceptPartyInvitationCommandHandler(inviteRepo, memberRepo, notifier);
        await sut.Handle(new AcceptPartyInvitationCommand(partyId, inviteId, authId), CancellationToken.None);

        await memberRepo.Received(1).AddAsync(Arg.Any<PartyMember>(), Arg.Any<CancellationToken>());
        await inviteRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.Status == InvitationStatus.Accepted), Arg.Any<CancellationToken>());
        await notifier.Received(1).SendInvitationAcceptedNotificationAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>());
    }
}
