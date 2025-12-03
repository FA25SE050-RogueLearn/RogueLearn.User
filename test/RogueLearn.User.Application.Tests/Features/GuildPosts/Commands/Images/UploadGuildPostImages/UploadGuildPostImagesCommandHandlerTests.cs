using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_NotMember_Throws(UploadGuildPostImagesCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new UploadGuildPostImagesCommandHandler(postRepo, memberRepo, storage);

        var post = new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId, AuthorId = cmd.AuthUserId };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_NotAuthor_Throws(UploadGuildPostImagesCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new UploadGuildPostImagesCommandHandler(postRepo, memberRepo, storage);

        var post = new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId, AuthorId = System.Guid.NewGuid() };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_SavesImagesAndReturnsUrls(UploadGuildPostImagesCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new UploadGuildPostImagesCommandHandler(postRepo, memberRepo, storage);

        var post = new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId, AuthorId = cmd.AuthUserId, Attachments = new Dictionary<string, object>() };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        storage.SaveImagesAsync(cmd.GuildId, cmd.PostId, Arg.Any<IEnumerable<(byte[] Content, string ContentType, string FileName)>>()!, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "https://img/a.png" });

        var res = await sut.Handle(cmd, CancellationToken.None);
        Assert.Contains("https://img/a.png", res.ImageUrls);
        await postRepo.Received(1).UpdateAsync(Arg.Any<GuildPost>(), Arg.Any<CancellationToken>());
    }
}