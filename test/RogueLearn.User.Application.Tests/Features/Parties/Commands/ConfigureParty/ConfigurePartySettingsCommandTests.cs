using FluentAssertions;
using RogueLearn.User.Application.Features.Parties.Commands.ConfigureParty;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.ConfigureParty;

public class ConfigurePartySettingsCommandTests
{
    [Fact]
    public void Record_Creates_With_Values()
    {
        var cmd = new ConfigurePartySettingsCommand(Guid.NewGuid(), "Name", "Desc", "public", 5);
        cmd.PartyId.Should().NotBe(Guid.Empty);
        cmd.Name.Should().Be("Name");
        cmd.Description.Should().Be("Desc");
        cmd.Privacy.Should().Be("public");
        cmd.MaxMembers.Should().Be(5);
    }
}

