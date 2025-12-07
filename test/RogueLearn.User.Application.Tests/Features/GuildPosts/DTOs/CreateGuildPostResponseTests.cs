using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.DTOs;

public class CreateGuildPostResponseTests
{
    [Fact]
    public void Property_Set_And_Read()
    {
        var r = new CreateGuildPostResponse { PostId = Guid.NewGuid() };
        r.PostId.Should().NotBe(Guid.Empty);
    }
}
