using RogueLearn.User.Application.Features.Parties.Commands.CreateParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.CreateParty;

public class CreatePartyResponseTests
{
    [Fact]
    public void MapFromEntity_ValidEntity_ReturnsDto()
    {
        var entity = new Party
        {
            Id = Guid.NewGuid(),
            Name = "Test Party",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var dto = new CreatePartyResponse
        {
            PartyId = entity.Id,
            RoleGranted = PartyRole.Leader.ToString(),
        };

        Assert.Equal(entity.Id, dto.PartyId);
        Assert.Equal(dto.RoleGranted, PartyRole.Leader.ToString());
    }
}