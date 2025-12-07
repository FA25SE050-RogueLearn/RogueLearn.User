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

    [Fact]
    public async Task Handle_Owner_SucceedsAndUpdatesCounts()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new DeleteGuildPostCommentCommandHandler(postRepo, commentRepo);

        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var cmd = new DeleteGuildPostCommentCommand(guildId, postId, commentId, requesterId);

        var post = new GuildPost { GuildId = guildId, Id = postId, CommentCount = 2 };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);

        var comment = new GuildPostComment { Id = commentId, PostId = postId, AuthorId = requesterId, Content = "x" };
        commentRepo.GetByIdAsync(commentId, Arg.Any<CancellationToken>()).Returns(comment);

        await sut.Handle(cmd, CancellationToken.None);

        await commentRepo.Received().UpdateAsync(Arg.Is<GuildPostComment>(c => c.DeletedAt.HasValue), Arg.Any<CancellationToken>());
        await postRepo.Received().UpdateAsync(Arg.Is<GuildPost>(p => p.CommentCount == 1), Arg.Any<CancellationToken>());
    }
}