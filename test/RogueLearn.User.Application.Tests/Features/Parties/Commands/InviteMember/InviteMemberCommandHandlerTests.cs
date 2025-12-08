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

    [Fact]
    public async Task Handle_CannotInviteSelf_ThrowsBadRequest()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var inviter = System.Guid.NewGuid();
        var target = new InviteTarget(inviter, null);
        var cmd = new InviteMemberCommand(System.Guid.NewGuid(), inviter, new[] { target }, "msg", System.DateTimeOffset.UtcNow.AddDays(2));

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PendingInvitationExists_ThrowsBadRequest()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var inviter = System.Guid.NewGuid();
        var invitee = System.Guid.NewGuid();
        var target = new InviteTarget(invitee, null);
        var cmd = new InviteMemberCommand(System.Guid.NewGuid(), inviter, new[] { target }, "msg", System.DateTimeOffset.UtcNow.AddDays(2));

        invRepo.GetByPartyAndInviteeAsync(cmd.PartyId, invitee, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { PartyId = cmd.PartyId, InviteeId = invitee, Status = RogueLearn.User.Domain.Enums.InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddDays(1) });

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingExpiredInvitation_IsUpdated()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var inviter = System.Guid.NewGuid();
        var invitee = System.Guid.NewGuid();
        var target = new InviteTarget(invitee, null);
        var cmd = new InviteMemberCommand(System.Guid.NewGuid(), inviter, new[] { target }, "msg", System.DateTimeOffset.UtcNow.AddDays(2));

        invRepo.GetByPartyAndInviteeAsync(cmd.PartyId, invitee, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { PartyId = cmd.PartyId, InviteeId = invitee, Status = RogueLearn.User.Domain.Enums.InvitationStatus.Pending, ExpiresAt = System.DateTimeOffset.UtcNow.AddSeconds(-1) });
        invRepo.UpdateAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyInvitation>());

        await sut.Handle(cmd, CancellationToken.None);
        await invRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.Status == RogueLearn.User.Domain.Enums.InvitationStatus.Pending && i.InvitedAt <= System.DateTimeOffset.UtcNow && i.RespondedAt == null), Arg.Any<CancellationToken>());
        await notifService.Received(1).SendInvitationNotificationAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingTarget_ThrowsBadRequest()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var inviter = System.Guid.NewGuid();
        var target = new InviteTarget(null, null);
        var cmd = new InviteMemberCommand(System.Guid.NewGuid(), inviter, new[] { target }, "msg", System.DateTimeOffset.UtcNow.AddDays(2));

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmailNotFound_ThrowsBadRequest()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var inviter = System.Guid.NewGuid();
        var target = new InviteTarget(null, "missing@example.com");
        var cmd = new InviteMemberCommand(System.Guid.NewGuid(), inviter, new[] { target }, "msg", System.DateTimeOffset.UtcNow.AddDays(2));

        userRepo.GetByEmailAsync("missing@example.com", Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmailNotFound_Throws()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var target = new InviteTarget(null, "missing@example.com");
        var cmd = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { target }, "msg", DateTimeOffset.UtcNow.AddDays(2));

        userRepo.GetByEmailAsync(target.Email!, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingExpired_UpdatesAndNotifies()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var invitee = new UserProfile { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Email = "target@example.com" };
        var target = new InviteTarget(null, invitee.Email);
        var cmd = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { target }, "msg", DateTimeOffset.UtcNow.AddDays(2));

        userRepo.GetByEmailAsync(invitee.Email, Arg.Any<CancellationToken>()).Returns(invitee);
        var existing = new PartyInvitation { PartyId = cmd.PartyId, InviteeId = invitee.AuthUserId, Status = InvitationStatus.Declined, ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) };
        invRepo.GetByPartyAndInviteeAsync(cmd.PartyId, invitee.AuthUserId, Arg.Any<CancellationToken>()).Returns(existing);
        invRepo.UpdateAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyInvitation>());

        await sut.Handle(cmd, CancellationToken.None);
        await invRepo.Received(1).UpdateAsync(Arg.Is<PartyInvitation>(i => i.Status == InvitationStatus.Pending && i.InviterId == cmd.InviterAuthUserId), Arg.Any<CancellationToken>());
        await notifService.Received(1).SendInvitationNotificationAsync(Arg.Any<PartyInvitation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PendingInvitation_Throws()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var invitee = new UserProfile { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Email = "target@example.com" };
        var target = new InviteTarget(null, invitee.Email);
        var cmd = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { target }, "msg", DateTimeOffset.UtcNow.AddDays(2));

        userRepo.GetByEmailAsync(invitee.Email, Arg.Any<CancellationToken>()).Returns(invitee);
        invRepo.GetByPartyAndInviteeAsync(cmd.PartyId, invitee.AuthUserId, Arg.Any<CancellationToken>()).Returns(new PartyInvitation { Status = InvitationStatus.Pending, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidTarget_Throws()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var target = new InviteTarget(null, null);
        var cmd = new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { target }, "msg", DateTimeOffset.UtcNow.AddDays(2));
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SelfInvite_Throws()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var inviter = Guid.NewGuid();
        var target = new InviteTarget(inviter, null);
        var cmd = new InviteMemberCommand(Guid.NewGuid(), inviter, new[] { target }, "msg", DateTimeOffset.UtcNow.AddDays(2));
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoInviteeIdValue_Throws()
    {
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var notifService = Substitute.For<IPartyNotificationService>();
        var userRepo = Substitute.For<IUserProfileRepository>();
        var sut = new InviteMemberCommandHandler(invRepo, notifService, userRepo);

        var inviter = Guid.NewGuid();
        var target = new InviteTarget(inviter, null);
        var cmd = new InviteMemberCommand(Guid.NewGuid(), inviter, new[] { target }, null, DateTimeOffset.UtcNow.AddDays(2));
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
