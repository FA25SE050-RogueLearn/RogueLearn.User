using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.InviteMember;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.InviteMember;

public class InviteMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_InvalidInviteTarget_Throws()
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();

        var sut = new InviteMemberCommandHandler(invitationRepo, notification, userRepo);
        var cmd = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { new InviteTarget(null, null) }, null, DateTimeOffset.UtcNow.AddDays(7));

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("Invite target must include userId or email.");
    }

    [Fact]
    public async Task Handle_EmailNotFound_Throws()
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();

        var sut = new InviteMemberCommandHandler(invitationRepo, notification, userRepo);
        userRepo.GetByEmailAsync("a@b.c", Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        var cmd = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { new InviteTarget(null, "a@b.c") }, "msg", DateTimeOffset.UtcNow.AddDays(7));

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("No user found with email 'a@b.c'.");
    }

    [Fact]
    public async Task Handle_SelfInvite_Throws()
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var inviter = Guid.NewGuid();

        var sut = new InviteMemberCommandHandler(invitationRepo, notification, userRepo);
        var cmd = new InviteMemberCommand(Guid.NewGuid(), inviter, new[] { new InviteTarget(inviter, null) }, "msg", DateTimeOffset.UtcNow.AddDays(7));

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("Cannot invite yourself to the party.");
    }

    [Fact]
    public async Task Handle_ExistingPendingInvitation_Throws()
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var partyId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var invitee = Guid.NewGuid();

        var existing = new PartyInvitation { PartyId = partyId, InviteeId = invitee, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        invitationRepo.GetByPartyAndInviteeAsync(partyId, invitee, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new InviteMemberCommandHandler(invitationRepo, notification, userRepo);
        var cmd = new InviteMemberCommand(partyId, inviter, new[] { new InviteTarget(invitee, null) }, "msg", DateTimeOffset.UtcNow.AddDays(7));

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("An invitation is already pending for this user.");
    }

    [Fact]
    public async Task Handle_UpdateExistingInvitation_SendsNotification()
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var partyId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var invitee = Guid.NewGuid();

        var existing = new PartyInvitation { PartyId = partyId, InviteeId = invitee, Status = InvitationStatus.Declined, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        invitationRepo.GetByPartyAndInviteeAsync(partyId, invitee, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new InviteMemberCommandHandler(invitationRepo, notification, userRepo);
        var newExpiry = DateTimeOffset.UtcNow.AddDays(10);
        var cmd = new InviteMemberCommand(partyId, inviter, new[] { new InviteTarget(invitee, null) }, "updated", newExpiry);

        await sut.Handle(cmd, CancellationToken.None);
        await invitationRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.InviterId == inviter && i.Message == "updated" && i.Status == InvitationStatus.Pending && i.RespondedAt == null && i.ExpiresAt == newExpiry), Arg.Any<CancellationToken>());
        await notification.Received(1).SendInvitationNotificationAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateExpiredPendingInvitation_SendsNotification()
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var partyId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var invitee = Guid.NewGuid();

        var existing = new PartyInvitation { PartyId = partyId, InviteeId = invitee, Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) };
        invitationRepo.GetByPartyAndInviteeAsync(partyId, invitee, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new InviteMemberCommandHandler(invitationRepo, notification, userRepo);
        var newExpiry = DateTimeOffset.UtcNow.AddDays(3);
        var cmd = new InviteMemberCommand(partyId, inviter, new[] { new InviteTarget(invitee, null) }, "again", newExpiry);

        await sut.Handle(cmd, CancellationToken.None);
        await invitationRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.Status == InvitationStatus.Pending && i.ExpiresAt == newExpiry), Arg.Any<CancellationToken>());
        await notification.Received(1).SendInvitationNotificationAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreateNewInvitation_SendsNotification()
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var partyId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var invitee = Guid.NewGuid();

        invitationRepo.GetByPartyAndInviteeAsync(partyId, invitee, Arg.Any<CancellationToken>()).Returns((PartyInvitation?)null);

        var sut = new InviteMemberCommandHandler(invitationRepo, notification, userRepo);
        var expiry = DateTimeOffset.UtcNow.AddDays(7);
        var cmd = new InviteMemberCommand(partyId, inviter, new[] { new InviteTarget(invitee, null) }, "msg", expiry);

        await sut.Handle(cmd, CancellationToken.None);
        await invitationRepo.Received(1).AddAsync(Arg.Is<PartyInvitation>(i => i.PartyId == partyId && i.InviterId == inviter && i.InviteeId == invitee && i.Message == "msg" && i.Status == InvitationStatus.Pending && i.ExpiresAt == expiry), Arg.Any<CancellationToken>());
        await notification.Received(1).SendInvitationNotificationAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailMapsToUser_CreatesInvitation()
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var partyId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var invitee = Guid.NewGuid();

        userRepo.GetByEmailAsync("z@x.y", Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = invitee });
        invitationRepo.GetByPartyAndInviteeAsync(partyId, invitee, Arg.Any<CancellationToken>()).Returns((PartyInvitation?)null);

        var sut = new InviteMemberCommandHandler(invitationRepo, notification, userRepo);
        var expiry = DateTimeOffset.UtcNow.AddDays(7);
        var cmd = new InviteMemberCommand(partyId, inviter, new[] { new InviteTarget(null, "z@x.y") }, "msg", expiry);

        await sut.Handle(cmd, CancellationToken.None);
        await invitationRepo.Received(1).AddAsync(Arg.Is<PartyInvitation>(i => i.InviteeId == invitee), Arg.Any<CancellationToken>());
        await notification.Received(1).SendInvitationNotificationAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidInviteTarget_FallbackGuardUnreachable_ButCoveredByEarlierChecks()
    {
        var invitationRepo = Substitute.For<IPartyInvitationRepository>();
        var notification = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();

        var sut = new InviteMemberCommandHandler(invitationRepo, notification, userRepo);
        var cmdMissing = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { new InviteTarget(null, null) }, null, DateTimeOffset.UtcNow.AddDays(7));
        var actMissing = () => sut.Handle(cmdMissing, CancellationToken.None);
        await actMissing.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("Invite target must include userId or email.");

        userRepo.GetByEmailAsync("x@y.z", Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var cmdUnknown = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { new InviteTarget(null, "x@y.z") }, null, DateTimeOffset.UtcNow.AddDays(7));
        var actUnknown = () => sut.Handle(cmdUnknown, CancellationToken.None);
        await actUnknown.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>().WithMessage("No user found with email 'x@y.z'.");
    }
}
