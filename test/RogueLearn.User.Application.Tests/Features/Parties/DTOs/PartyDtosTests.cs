using FluentAssertions;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Commands.InviteMember;

namespace RogueLearn.User.Application.Tests.Features.Parties.DTOs;

public class PartyDtosTests
{
    [Fact]
    public void AddPartyResourceRequest_SetsFields()
    {
        var r = new AddPartyResourceRequest(Guid.NewGuid(), "t", new { a = 1 }, new[] { "x" });
        r.Title.Should().Be("t");
        r.Tags.Should().Contain("x");
    }

    [Fact]
    public void InviteMemberRequest_SetsTargets()
    {
        var req = new InviteMemberRequest(new[] { new InviteTarget(Guid.NewGuid(), null) }, "m", DateTimeOffset.UtcNow.AddDays(1));
        req.Targets.Should().HaveCount(1);
        req.Message.Should().Be("m");
    }
}