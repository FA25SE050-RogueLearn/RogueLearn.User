using FluentAssertions;
using RogueLearn.User.Application.Features.Parties.Commands.CreateParty;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.CreateParty;

public class CreatePartyValidatorTests
{
    [Fact]
    public void Invalid_WhenNameEmptyOrMaxMembersOutOfRange()
    {
        var validator = new CreatePartyCommandValidator();
        var cmd = new CreatePartyCommand { CreatorAuthUserId = Guid.Empty, Name = "", MaxMembers = 2 };
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_WhenFieldsProvided()
    {
        var validator = new CreatePartyCommandValidator();
        var cmd = new CreatePartyCommand { CreatorAuthUserId = Guid.NewGuid(), Name = "P", MaxMembers = 6 };
        var res = validator.Validate(cmd);
        res.IsValid.Should().BeTrue();
    }
}