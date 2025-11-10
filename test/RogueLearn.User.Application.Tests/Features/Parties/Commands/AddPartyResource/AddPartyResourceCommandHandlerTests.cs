using FluentAssertions;
using Moq;
using AutoMapper;
using RogueLearn.User.Application.Features.Parties.Commands.AddPartyResource;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.Parties.DTOs;
using System.Text.Json;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.AddPartyResource;

public class AddPartyResourceCommandHandlerTests
{
    private readonly Mock<IPartyStashItemRepository> _stashRepository = new();
    private readonly Mock<IPartyNotificationService> _notificationService = new();
    private readonly Mock<IMapper> _mapper = new();

    private readonly AddPartyResourceCommandHandler _handler;

    public AddPartyResourceCommandHandlerTests()
    {
        _handler = new AddPartyResourceCommandHandler(
            _stashRepository.Object,
            _notificationService.Object,
            _mapper.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldAddStashItem_AndSendNotification()
    {
        // Arrange
        var cmd = new AddPartyResourceCommand(
            PartyId: Guid.NewGuid(),
            SharedByUserId: Guid.NewGuid(),
            OriginalNoteId: Guid.NewGuid(),
            Title: "Doc",
            Content: "{\"type\":\"document\",\"url\":\"https://example.com\"}",
            Tags: new List<string> { "study" }
        );

        var saved = new PartyStashItem
        {
            Id = Guid.NewGuid(),
            PartyId = cmd.PartyId,
            OriginalNoteId = cmd.OriginalNoteId,
            SharedByUserId = cmd.SharedByUserId,
            Title = cmd.Title,
            // Content is stored as a JSON string in the entity
            Content = JsonSerializer.Serialize(cmd.Content, (JsonSerializerOptions?)null),
            Tags = cmd.Tags.ToArray(),
            SharedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _stashRepository
            .Setup(r => r.AddAsync(It.IsAny<PartyStashItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        _mapper
            .Setup(m => m.Map<PartyStashItemDto>(It.IsAny<PartyStashItem>()))
            .Returns(new PartyStashItemDto(
                saved.Id,
                saved.PartyId,
                saved.OriginalNoteId,
                saved.SharedByUserId,
                saved.Title,
                saved.Content,
                saved.Tags,
                saved.SharedAt,
                saved.UpdatedAt));

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(saved.Id);

        _stashRepository.Verify(r => r.AddAsync(It.IsAny<PartyStashItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.SendMaterialUploadNotificationAsync(It.IsAny<PartyStashItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}