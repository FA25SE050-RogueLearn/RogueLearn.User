using RogueLearn.User.Application.Features.GuildPosts.DTOs;

﻿namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.CreateGuildPost;

public class CreateGuildPostResponseTests
{
    [Fact]
    public void CreateGuildPostResponse_ConstructsAndSerializes()
    {
        var dto = new CreateGuildPostResponse
        {
            PostId = Guid.NewGuid()
        };
        Assert.Equal(dto.PostId, dto.PostId);
    }
}