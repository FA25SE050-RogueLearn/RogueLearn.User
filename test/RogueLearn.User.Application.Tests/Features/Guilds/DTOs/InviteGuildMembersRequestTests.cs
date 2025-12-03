using FluentAssertions;
using RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Tests.Features.Guilds.DTOs;

public class InviteGuildMembersRequestTests
{
    [Fact]
    public void Request_CreatesWithTargetsAndMessage()
    {
        var targets = new List<InviteTarget> { new InviteTarget(Guid.NewGuid(), null), new InviteTarget(null, "e@x.com") };
        var r = new InviteGuildMembersRequest(targets, "hello");
        r.Targets.Should().HaveCount(2);
        r.Message.Should().Be("hello");
    }
}