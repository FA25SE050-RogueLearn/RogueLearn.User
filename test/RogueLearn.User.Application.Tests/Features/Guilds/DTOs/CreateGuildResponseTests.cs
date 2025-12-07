using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.Guilds.Commands.CreateGuild;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.DTOs;

public class CreateGuildResponseTests
{
    [Fact]
    public void Property_Set_And_Read()
    {
        var dto = new GuildDto { Id = Guid.NewGuid(), Name = "G" };
        var r = new CreateGuildResponse { GuildId = dto.Id, RoleGranted = "GuildMaster", Guild = dto };
        r.GuildId.Should().Be(dto.Id);
        r.RoleGranted.Should().Be("GuildMaster");
        r.Guild.Should().NotBeNull();
        r.Guild.Name.Should().Be("G");
    }
}

