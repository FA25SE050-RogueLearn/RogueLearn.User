using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.Comments.CreateGuildPostComment;

public class CreateGuildPostCommentResponseTests
{
    [Fact]
    public void CreateGuildPostCommentResponse_ConstructsAndSerializes()
    {
        var dto = new CreateGuildPostCommentResponse
        {
            CommentId = Guid.NewGuid()
        };
        Assert.Equal(dto.CommentId, dto.CommentId);
    }
}