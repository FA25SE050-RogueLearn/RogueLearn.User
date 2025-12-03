using System.Collections.Generic;
using FluentAssertions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Images;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.Images.UploadGuildPostImages;

public class UploadGuildPostImagesResponseTests
{
    [Fact]
    public void Property_Set_And_Read()
    {
        var r = new UploadGuildPostImagesResponse { ImageUrls = new List<string> { "u" } };
        r.ImageUrls.Should().Contain("u");
    }
}