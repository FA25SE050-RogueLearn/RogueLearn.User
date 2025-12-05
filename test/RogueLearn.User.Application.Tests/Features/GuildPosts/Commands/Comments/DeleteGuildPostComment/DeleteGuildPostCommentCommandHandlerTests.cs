using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.DeleteGuildPostComment;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.Comments.DeleteGuildPostComment;

public class DeleteGuildPostCommentCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotOwner_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new DeleteGuildPostCommentCommandHandler(postRepo, commentRepo);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var commentId = System.Guid.NewGuid();
        var requesterId = System.Guid.NewGuid();
        var cmd = new DeleteGuildPostCommentCommand(guildId, postId, commentId, requesterId);

        var post = new GuildPost { GuildId = guildId, Id = postId };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        var comment = new GuildPostComment { Id = commentId, PostId = postId, AuthorId = System.Guid.NewGuid() };
        commentRepo.GetByIdAsync(commentId, Arg.Any<CancellationToken>()).Returns(comment);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}