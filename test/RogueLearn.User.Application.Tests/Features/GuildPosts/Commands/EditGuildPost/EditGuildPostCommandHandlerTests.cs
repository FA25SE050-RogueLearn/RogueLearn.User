using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_WrongAuthor_Throws(EditGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new EditGuildPostCommandHandler(postRepo, storage);

        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, AuthorId = System.Guid.NewGuid(), IsLocked = false };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Locked_Throws(EditGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new EditGuildPostCommandHandler(postRepo, storage);

        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, AuthorId = cmd.AuthorAuthUserId, IsLocked = true };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Updates(EditGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new EditGuildPostCommandHandler(postRepo, storage);

        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, AuthorId = cmd.AuthorAuthUserId, IsLocked = false };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);

        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png") };
        var req = new EditGuildPostRequest { Title = "New", Content = "C", Tags = new[] { "t" }, Attachments = new Dictionary<string, object>(), Images = images };
        cmd = new EditGuildPostCommand(cmd.GuildId, cmd.PostId, cmd.AuthorAuthUserId, req);
        storage.SaveImagesAsync(cmd.GuildId, cmd.PostId, Arg.Any<IEnumerable<(byte[] Content, string ContentType, string FileName)>>()!, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "https://img" });

        await sut.Handle(cmd, CancellationToken.None);
        await postRepo.Received(1).UpdateAsync(Arg.Any<GuildPost>(), Arg.Any<CancellationToken>());
    }
}