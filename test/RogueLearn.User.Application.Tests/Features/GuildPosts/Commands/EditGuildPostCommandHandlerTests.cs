using System.Threading;
using System.Threading.Tasks;
using Moq;
using RogueLearn.User.Application.Features.GuildPosts.Commands.EditGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands;

public class EditGuildPostCommandHandlerTests
{
    [Fact]
    public async Task EditGuildPost_Should_Update_Title_And_Content()
    {
        var repo = new Mock<IGuildPostRepository>();
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildPost { Id = postId, GuildId = guildId, AuthorId = authorId });

        repo.Setup(r => r.UpdateAsync(It.IsAny<GuildPost>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildPost p, CancellationToken _) => p);

        var imageStorage = new Mock<RogueLearn.User.Application.Interfaces.IGuildPostImageStorage>();
        var handler = new EditGuildPostCommandHandler(repo.Object, imageStorage.Object);
        var cmd = new EditGuildPostCommand(guildId, postId, authorId, new EditGuildPostRequest
        {
            Title = "Updated",
            Content = "Updated content",
            Tags = new[] { "discussion" },
            Attachments = new Dictionary<string, object>()
        });

        await handler.Handle(cmd, CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(It.Is<GuildPost>(p => p.Title == "Updated" && p.Content == "Updated content"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditGuildPost_Should_Throw_When_Not_Author()
    {
        var repo = new Mock<IGuildPostRepository>();
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildPost { Id = postId, GuildId = guildId, AuthorId = Guid.NewGuid() });

        var imageStorage2 = new Mock<RogueLearn.User.Application.Interfaces.IGuildPostImageStorage>();
        var handler = new EditGuildPostCommandHandler(repo.Object, imageStorage2.Object);
        var cmd = new EditGuildPostCommand(guildId, postId, authorId, new EditGuildPostRequest { Title = "x" });

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(cmd, CancellationToken.None));
    }
}