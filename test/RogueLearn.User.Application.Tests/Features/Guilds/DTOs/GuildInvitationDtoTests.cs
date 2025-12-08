using RogueLearn.User.Application.Features.Guilds.DTOs;
using FluentAssertions;

namespace RogueLearn.User.Application.Tests.Features.Guilds.DTOs;

public class GuildInvitationDtoTests
{
    [Fact]
    public void Map_GuildInvitationDto_Fields_Are_Mapped()
    {
        var dto = new GuildInvitationDto
        (
            
        )
        {
            Id = Guid.NewGuid(),
            GuildId = Guid.NewGuid(),
            Message = "hello",
            InviteeId = Guid.NewGuid(),
        };
        dto.Id.Should().NotBeEmpty();
        dto.GuildId.Should().NotBeEmpty();
        dto.Message.Should().NotBeEmpty();
        dto.InviteeId.Should().NotBeEmpty();
    }
}