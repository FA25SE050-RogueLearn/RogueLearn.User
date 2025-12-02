using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_PostLocked_Throws(EditGuildPostCommentCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new EditGuildPostCommentCommandHandler(postRepo, commentRepo);

        var post = new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId, IsLocked = true };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_NotOwnerOrMismatch_Throws(EditGuildPostCommentCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new EditGuildPostCommentCommandHandler(postRepo, commentRepo);

        var post = new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId, IsLocked = false };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        commentRepo.GetByIdAsync(cmd.CommentId, Arg.Any<CancellationToken>()).Returns(new GuildPostComment { PostId = cmd.PostId, AuthorId = System.Guid.NewGuid() });

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}