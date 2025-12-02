using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.CreateGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.CreateGuildPost;

public class CreateGuildPostCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotMember_Throws(CreateGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new CreateGuildPostCommandHandler(postRepo, memberRepo, guildRepo, storage);

        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, RequiresApproval = false });
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthorAuthUserId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_InactiveMember_Throws(CreateGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new CreateGuildPostCommandHandler(postRepo, memberRepo, guildRepo, storage);

        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, RequiresApproval = false });
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthorAuthUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember { Status = MemberStatus.Inactive });

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_SavesImagesAndAddsPost(CreateGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new CreateGuildPostCommandHandler(postRepo, memberRepo, guildRepo, storage);

        var guild = new Guild { Id = cmd.GuildId, RequiresApproval = false };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthorAuthUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember { Status = MemberStatus.Active });

        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png") };
        var request = new CreateGuildPostRequest { Title = "T", Content = "C", Tags = new[] { "x" }, Attachments = new Dictionary<string, object>(), Images = images };
        cmd = new CreateGuildPostCommand(cmd.GuildId, cmd.AuthorAuthUserId, request);

        storage.SaveImagesAsync(cmd.GuildId, Arg.Any<System.Guid>(), Arg.Any<IEnumerable<(byte[] Content, string ContentType, string FileName)>>()!, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "https://img" });

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.PostId.Should().NotBe(Guid.Empty);
        await postRepo.Received(1).AddAsync(Arg.Any<GuildPost>(), Arg.Any<CancellationToken>());
    }
}