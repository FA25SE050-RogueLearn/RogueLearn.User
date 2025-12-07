using System.Collections.Generic;
using FluentAssertions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Images;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.DTOs;

public class UploadGuildPostImagesResponseTests
{
    [Fact]
    public void Property_Set_And_Read()
    {
        var r = new UploadGuildPostImagesResponse { ImageUrls = new List<string> { "a.png", "b.jpg" } };
        r.ImageUrls.Should().HaveCount(2).And.Contain(new[] { "a.png", "b.jpg" });
    }
}

