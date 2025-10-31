using FluentAssertions;
using Moq;
using AutoMapper;
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
    private readonly Mock<IMapper> _mapper = new();

    private readonly InviteMemberCommandHandler _handler;

    public InviteMemberCommandHandlerTests()
    {
        _handler = new InviteMemberCommandHandler(
            _invitationRepository.Object,
            _notificationService.Object,
            _mapper.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateInvitation_AndSendNotification()
    {
        // Arrange
        var cmd = new InviteMemberCommand(
            PartyId: Guid.NewGuid(),
            InviterAuthUserId: Guid.NewGuid(),
            InviteeAuthUserId: Guid.NewGuid(),
            Message: "Join us!",
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(3)
        );

        var saved = new PartyInvitation { Id = Guid.NewGuid(), PartyId = cmd.PartyId, InviterId = cmd.InviterAuthUserId, InviteeId = cmd.InviteeAuthUserId, Status = InvitationStatus.Pending, ExpiresAt = cmd.ExpiresAt };

        _invitationRepository
            .Setup(r => r.AddAsync(It.IsAny<PartyInvitation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        _mapper
            .Setup(m => m.Map<PartyInvitationDto>(It.IsAny<PartyInvitation>()))
            .Returns(new PartyInvitationDto(saved.Id, saved.PartyId, saved.InviterId, saved.InviteeId, saved.Status, saved.Message, saved.ExpiresAt, saved.InvitedAt));

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(saved.Id);

        _invitationRepository.Verify(r => r.AddAsync(It.IsAny<PartyInvitation>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.SendInvitationNotificationAsync(It.IsAny<PartyInvitation>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}