using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.EditGuildPostComment;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.Comments.EditGuildPostComment;

public class EditGuildPostCommentCommandHandlerTests
{
    [Fact]
    public async Task Handle_PostLocked_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new EditGuildPostCommentCommandHandler(postRepo, commentRepo);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var commentId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var req = new EditGuildPostCommentRequest { Content = "c" };
        var cmd = new EditGuildPostCommentCommand
        {
            GuildId = guildId,
            PostId = postId,
            CommentId = commentId,
            AuthorId = authorId,
            Request = req
        };

        var post = new GuildPost { GuildId = guildId, Id = postId, IsLocked = true };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrMismatch_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new EditGuildPostCommentCommandHandler(postRepo, commentRepo);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var commentId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var req = new EditGuildPostCommentRequest { Content = "c" };
        var cmd = new EditGuildPostCommentCommand
        {
            GuildId = guildId,
            PostId = postId,
            CommentId = commentId,
            AuthorId = authorId,
            Request = req
        };

        var post = new GuildPost { GuildId = guildId, Id = postId, IsLocked = false };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        commentRepo.GetByIdAsync(commentId, Arg.Any<CancellationToken>()).Returns(new GuildPostComment { PostId = postId, AuthorId = System.Guid.NewGuid() });

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OwnerAndUnlocked_UpdatesContent()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new EditGuildPostCommentCommandHandler(postRepo, commentRepo);

        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var req = new EditGuildPostCommentRequest { Content = "new content" };
        var cmd = new EditGuildPostCommentCommand
        {
            GuildId = guildId,
            PostId = postId,
            CommentId = commentId,
            AuthorId = authorId,
            Request = req
        };

        var post = new GuildPost { GuildId = guildId, Id = postId, IsLocked = false };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);

        var comment = new GuildPostComment { Id = commentId, PostId = postId, AuthorId = authorId, Content = "old" };
        commentRepo.GetByIdAsync(commentId, Arg.Any<CancellationToken>()).Returns(comment);

        await sut.Handle(cmd, CancellationToken.None);

        await commentRepo.Received().UpdateAsync(Arg.Is<GuildPostComment>(c => c.Content == "new content"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommentNotFound_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new EditGuildPostCommentCommandHandler(postRepo, commentRepo);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var commentId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var req = new EditGuildPostCommentRequest { Content = "c" };
        var cmd = new EditGuildPostCommentCommand
        {
            GuildId = guildId,
            PostId = postId,
            CommentId = commentId,
            AuthorId = authorId,
            Request = req
        };

        var post = new GuildPost { GuildId = guildId, Id = postId, IsLocked = false };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        commentRepo.GetByIdAsync(commentId, Arg.Any<CancellationToken>()).Returns((GuildPostComment?)null);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
