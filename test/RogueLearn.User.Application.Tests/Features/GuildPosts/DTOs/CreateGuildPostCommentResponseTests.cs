using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.DTOs;

public class CreateGuildPostCommentResponseTests
{
    [Fact]
    public void Property_Set_And_Read()
    {
        var r = new CreateGuildPostCommentResponse { CommentId = Guid.NewGuid() };
        r.CommentId.Should().NotBe(Guid.Empty);
    }
}

