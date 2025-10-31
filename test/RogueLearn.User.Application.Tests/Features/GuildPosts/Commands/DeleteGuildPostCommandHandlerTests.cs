using System.Threading;
using System.Threading.Tasks;
using Moq;
using RogueLearn.User.Application.Features.GuildPosts.Commands.DeleteGuildPost;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands;

public class DeleteGuildPostCommandHandlerTests
{
    [Fact]
    public async Task DeleteGuildPost_Should_Delete_When_Author()
    {
        var repo = new Mock<IGuildPostRepository>();
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildPost { Id = postId, GuildId = guildId, AuthorId = authorId });

        repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteGuildPostCommandHandler(repo.Object);
        await handler.Handle(new DeleteGuildPostCommand(guildId, postId, authorId, false), CancellationToken.None);

        repo.Verify(r => r.DeleteAsync(It.Is<Guid>(id => id == postId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGuildPost_Should_Throw_When_Not_Author_And_Not_Forced()
    {
        var repo = new Mock<IGuildPostRepository>();
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var requester = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildPost { Id = postId, GuildId = guildId, AuthorId = authorId });

        var handler = new DeleteGuildPostCommandHandler(repo.Object);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new DeleteGuildPostCommand(guildId, postId, requester, false), CancellationToken.None));
    }
}