using FluentAssertions;
using Moq;
using AutoMapper;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Tests.Features.Parties.Queries.GetPartyById;

public class GetPartyByIdQueryHandlerTests
{
    private readonly Mock<IPartyRepository> _partyRepository = new();
    private readonly Mock<IMapper> _mapper = new();

    private readonly GetPartyByIdQueryHandler _handler;

    public GetPartyByIdQueryHandlerTests()
    {
        _handler = new GetPartyByIdQueryHandler(_partyRepository.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WhenPartyExists_ReturnsDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var party = new Party { Id = id, Name = "Test" };
        _partyRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(party);
        _mapper.Setup(m => m.Map<PartyDto>(party))
               .Returns(new PartyDto(id, party.Name, party.Description ?? string.Empty, party.PartyType, party.MaxMembers, party.IsPublic, party.CreatedBy, party.CreatedAt));

        // Act
        var result = await _handler.Handle(new GetPartyByIdQuery(id), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task Handle_WhenPartyMissing_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _partyRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Party?)null);

        // Act
        var result = await _handler.Handle(new GetPartyByIdQuery(id), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}