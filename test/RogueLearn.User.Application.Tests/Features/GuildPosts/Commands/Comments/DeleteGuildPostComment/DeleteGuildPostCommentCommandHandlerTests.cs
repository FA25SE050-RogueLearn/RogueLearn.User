using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.DeleteGuildPostComment;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.Comments.DeleteGuildPostComment;

public class DeleteGuildPostCommentCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotOwner_Throws(DeleteGuildPostCommentCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var sut = new DeleteGuildPostCommentCommandHandler(postRepo, commentRepo);

        var post = new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        var comment = new GuildPostComment { Id = cmd.CommentId, PostId = cmd.PostId, AuthorId = System.Guid.NewGuid() };
        commentRepo.GetByIdAsync(cmd.CommentId, Arg.Any<CancellationToken>()).Returns(comment);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}