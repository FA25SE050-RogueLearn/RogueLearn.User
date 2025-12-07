using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Images;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.Images.UploadGuildPostImages;

public class UploadGuildPostImagesCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotMember_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new UploadGuildPostImagesCommandHandler(postRepo, memberRepo, storage);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var authUserId = System.Guid.NewGuid();
        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png") };
        var cmd = new UploadGuildPostImagesCommand
        {
            GuildId = guildId,
            PostId = postId,
            AuthUserId = authUserId,
            Images = images
        };

        var post = new GuildPost { GuildId = guildId, Id = postId, AuthorId = authUserId };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        memberRepo.GetMemberAsync(guildId, authUserId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotAuthor_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new UploadGuildPostImagesCommandHandler(postRepo, memberRepo, storage);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var authUserId = System.Guid.NewGuid();
        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png") };
        var cmd = new UploadGuildPostImagesCommand
        {
            GuildId = guildId,
            PostId = postId,
            AuthUserId = authUserId,
            Images = images
        };

        var post = new GuildPost { GuildId = guildId, Id = postId, AuthorId = System.Guid.NewGuid() };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        memberRepo.GetMemberAsync(guildId, authUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_SavesImagesAndReturnsUrls()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new UploadGuildPostImagesCommandHandler(postRepo, memberRepo, storage);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var authUserId = System.Guid.NewGuid();
        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png") };
        var cmd = new UploadGuildPostImagesCommand
        {
            GuildId = guildId,
            PostId = postId,
            AuthUserId = authUserId,
            Images = images
        };

        var post = new GuildPost { GuildId = guildId, Id = postId, AuthorId = authUserId, Attachments = new Dictionary<string, object>() };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        memberRepo.GetMemberAsync(guildId, authUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        storage.SaveImagesAsync(guildId, postId, Arg.Any<IEnumerable<(byte[] Content, string ContentType, string FileName)>>()!, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "https://img/a.png" });

        var res = await sut.Handle(cmd, CancellationToken.None);
        Assert.Contains("https://img/a.png", res.ImageUrls);
        await postRepo.Received(1).UpdateAsync(Arg.Any<GuildPost>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LockedPost_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new UploadGuildPostImagesCommandHandler(postRepo, memberRepo, storage);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var authUserId = System.Guid.NewGuid();
        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png") };
        var cmd = new UploadGuildPostImagesCommand
        {
            GuildId = guildId,
            PostId = postId,
            AuthUserId = authUserId,
            Images = images
        };

        var post = new GuildPost { GuildId = guildId, Id = postId, AuthorId = authUserId, IsLocked = true };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        memberRepo.GetMemberAsync(guildId, authUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MergesExistingImages()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new UploadGuildPostImagesCommandHandler(postRepo, memberRepo, storage);

        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var authUserId = System.Guid.NewGuid();
        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png") };
        var cmd = new UploadGuildPostImagesCommand
        {
            GuildId = guildId,
            PostId = postId,
            AuthUserId = authUserId,
            Images = images
        };

        var attachments = new Dictionary<string, object> { ["images"] = new List<object> { "https://img/old.png" } };
        var post = new GuildPost { GuildId = guildId, Id = postId, AuthorId = authUserId, Attachments = attachments };
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        memberRepo.GetMemberAsync(guildId, authUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        storage.SaveImagesAsync(guildId, postId, Arg.Any<IEnumerable<(byte[] Content, string ContentType, string FileName)>>()!, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "https://img/new.png" });

        var res = await sut.Handle(cmd, CancellationToken.None);
        Assert.Contains("https://img/new.png", res.ImageUrls);
        await postRepo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => ((List<object>)p.Attachments!["images"]).Count == 2), Arg.Any<CancellationToken>());
    }
}
