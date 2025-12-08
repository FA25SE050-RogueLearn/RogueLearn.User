using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.Parties.Commands.CreateParty;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.DTOs;

public class CreatePartyResponseTests
{
    [Fact]
    public void Property_Set_And_Read()
    {
        var r = new CreatePartyResponse { PartyId = Guid.NewGuid(), RoleGranted = "PartyLeader" };
        r.PartyId.Should().NotBe(Guid.Empty);
        r.RoleGranted.Should().Be("PartyLeader");
    }
}

