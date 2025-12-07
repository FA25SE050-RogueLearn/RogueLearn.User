using System.Collections.Generic;
using FluentAssertions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Images;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.Images;

public class UploadGuildPostImagesCommandTests
{
    [Fact]
    public void Type_Constructs_With_Values()
    {
        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var authUserId = System.Guid.NewGuid();
        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[]{1}, "image/png", "a.png") };
        var cmd = new UploadGuildPostImagesCommand { GuildId = guildId, PostId = postId, AuthUserId = authUserId, Images = images };
        cmd.GuildId.Should().Be(guildId);
        cmd.PostId.Should().Be(postId);
        cmd.AuthUserId.Should().Be(authUserId);
        cmd.Images.Should().HaveCount(1);
    }
}
