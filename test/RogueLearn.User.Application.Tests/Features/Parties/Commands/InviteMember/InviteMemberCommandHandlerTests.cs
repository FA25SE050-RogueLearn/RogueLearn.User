using FluentAssertions;
using Moq;
using MediatR;
using RogueLearn.User.Application.Features.Parties.Commands.InviteMember;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.InviteMember;

public class InviteMemberCommandHandlerTests
{
    private readonly Mock<IPartyInvitationRepository> _invitationRepository = new();
    private readonly Mock<IPartyNotificationService> _notificationService = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();

    private readonly InviteMemberCommandHandler _handler;

    public InviteMemberCommandHandlerTests()
    {
        _handler = new InviteMemberCommandHandler(
            _invitationRepository.Object,
            _notificationService.Object,
            _userProfileRepository.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateInvitation_AndSendNotification()
    {
        // Arrange
        var partyId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var cmd = new InviteMemberCommand(
            PartyId: partyId,
            InviterAuthUserId: inviterId,
            Targets: new[] { new InviteTarget(inviteeId, null) },
            Message: "Join us!",
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(3)
        );

        var saved = new PartyInvitation { Id = Guid.NewGuid(), PartyId = partyId, InviterId = inviterId, InviteeId = inviteeId, Status = InvitationStatus.Pending, ExpiresAt = cmd.ExpiresAt };

        _invitationRepository
            .Setup(r => r.AddAsync(It.IsAny<PartyInvitation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        // No mapping needed; handler returns Unit

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _invitationRepository.Verify(r => r.AddAsync(It.IsAny<PartyInvitation>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.SendInvitationNotificationAsync(It.IsAny<PartyInvitation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenInviterEqualsInvitee()
    {
        var partyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cmd = new InviteMemberCommand(
            PartyId: partyId,
            InviterAuthUserId: userId,
            Targets: new[] { new InviteTarget(userId, null) },
            Message: "Join",
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(3)
        );

        _invitationRepository
            .Setup(r => r.GetPendingInvitationsByPartyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartyInvitation>());
        Func<Task> act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
        _invitationRepository.Verify(r => r.AddAsync(It.IsAny<PartyInvitation>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(n => n.SendInvitationNotificationAsync(It.IsAny<PartyInvitation>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}