using FluentAssertions;
using RogueLearn.User.Application.Features.Parties.Commands.ConfigureParty;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.ConfigureParty;

public class ConfigurePartyValidatorTests
{
    [Fact]
    public void Invalid_WhenFieldsMissing()
    {
        var validator = new ConfigurePartySettingsCommandValidator();
        var cmd = new ConfigurePartySettingsCommand(Guid.Empty, "", "", "", 2);
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_WhenFieldsValid()
    {
        var validator = new ConfigurePartySettingsCommandValidator();
        var cmd = new ConfigurePartySettingsCommand(Guid.NewGuid(), "N", "D", "public", 6);
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeTrue();
    }
}