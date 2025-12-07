using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.DTOs;

public class CreateGuildPostCommentRequestTests
{
    [Fact]
    public void Properties_Set_And_Read()
    {
        var parent = Guid.NewGuid();
        var r = new CreateGuildPostCommentRequest { Content = "text", ParentCommentId = parent };
        r.Content.Should().Be("text");
        r.ParentCommentId.Should().Be(parent);
    }
}

