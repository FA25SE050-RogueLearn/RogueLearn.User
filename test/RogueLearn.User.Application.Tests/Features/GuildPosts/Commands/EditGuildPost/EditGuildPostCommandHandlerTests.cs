using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.EditGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.EditGuildPost;

public class EditGuildPostCommandHandlerTests
{
    [Fact]
    public async Task Handle_WrongAuthor_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new EditGuildPostCommandHandler(postRepo, storage);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var wrongAuthor = System.Guid.NewGuid();
        var req = new EditGuildPostRequest { Title = "T", Content = "C", Tags = new[] { "t" }, Attachments = new Dictionary<string, object>(), Images = new List<GuildPostImageUpload>() };
        var cmd = new EditGuildPostCommand(guildId, postId, authorId, req);

        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = wrongAuthor, IsLocked = false };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Locked_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new EditGuildPostCommandHandler(postRepo, storage);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var req = new EditGuildPostRequest { Title = "T", Content = "C", Tags = new[] { "t" }, Attachments = new Dictionary<string, object>(), Images = new List<GuildPostImageUpload>() };
        var cmd = new EditGuildPostCommand(guildId, postId, authorId, req);

        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = authorId, IsLocked = true };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Updates()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new EditGuildPostCommandHandler(postRepo, storage);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();

        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = authorId, IsLocked = false };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);

        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png") };
        var req = new EditGuildPostRequest { Title = "New", Content = "C", Tags = new[] { "t" }, Attachments = new Dictionary<string, object>(), Images = images };
        var cmd = new EditGuildPostCommand(guildId, postId, authorId, req);
        storage.SaveImagesAsync(guildId, postId, Arg.Any<IEnumerable<(byte[] Content, string ContentType, string FileName)>>()!, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "https://img" });

        await sut.Handle(cmd, CancellationToken.None);
        await postRepo.Received(1).UpdateAsync(Arg.Any<GuildPost>(), Arg.Any<CancellationToken>());
    }
}