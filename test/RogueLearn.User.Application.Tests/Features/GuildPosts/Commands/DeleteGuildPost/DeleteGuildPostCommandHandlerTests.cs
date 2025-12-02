using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.DeleteGuildPost;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.DeleteGuildPost;

public class DeleteGuildPostCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_WrongAuthor_Throws(DeleteGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new DeleteGuildPostCommandHandler(postRepo, storage);

        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, AuthorId = System.Guid.NewGuid(), IsLocked = false };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Locked_Throws(DeleteGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new DeleteGuildPostCommandHandler(postRepo, storage);

        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, AuthorId = cmd.RequesterAuthUserId, IsLocked = true };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_WithImages_DeletesAndCallsRepo(DeleteGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new DeleteGuildPostCommandHandler(postRepo, storage);

        var attachments = new Dictionary<string, object> { ["images"] = new List<object> { "https://img1", "https://img2" } };
        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, AuthorId = cmd.RequesterAuthUserId, IsLocked = false, Attachments = attachments };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);

        await sut.Handle(cmd, CancellationToken.None);
        await storage.Received(1).DeleteByUrlsAsync(Arg.Is<IEnumerable<string>>(u => u.Count() == 2), Arg.Any<CancellationToken>());
        await postRepo.Received(1).DeleteAsync(cmd.PostId, Arg.Any<CancellationToken>());
    }
}