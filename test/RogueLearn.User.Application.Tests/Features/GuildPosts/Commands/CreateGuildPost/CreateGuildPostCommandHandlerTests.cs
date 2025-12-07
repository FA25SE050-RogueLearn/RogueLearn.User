using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    [Fact]
    public async Task Handle_NotMember_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new CreateGuildPostCommandHandler(postRepo, memberRepo, guildRepo, storage);

        var cmd = new CreateGuildPostCommand
        {
            GuildId = Guid.NewGuid(),
            AuthorAuthUserId = Guid.NewGuid(),
            Request = new CreateGuildPostRequest { Title = "T", Content = "C" }
        };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, RequiresApproval = false });
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthorAuthUserId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InactiveMember_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new CreateGuildPostCommandHandler(postRepo, memberRepo, guildRepo, storage);

        var cmd = new CreateGuildPostCommand
        {
            GuildId = Guid.NewGuid(),
            AuthorAuthUserId = Guid.NewGuid(),
            Request = new CreateGuildPostRequest { Title = "T", Content = "C" }
        };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, RequiresApproval = false });
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthorAuthUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember { Status = MemberStatus.Inactive });

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SavesImagesAndAddsPost()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new CreateGuildPostCommandHandler(postRepo, memberRepo, guildRepo, storage);

        var cmd = new CreateGuildPostCommand
        {
            GuildId = Guid.NewGuid(),
            AuthorAuthUserId = Guid.NewGuid(),
            Request = new CreateGuildPostRequest { Title = "T", Content = "C" }
        };
        var guild = new Guild { Id = cmd.GuildId, RequiresApproval = false };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthorAuthUserId, Arg.Any<CancellationToken>()).Returns(new GuildMember { Status = MemberStatus.Active });

        var images = new List<GuildPostImageUpload> { new GuildPostImageUpload(new byte[] { 1 }, "image/png", "a.png") };
        var request = new CreateGuildPostRequest { Title = "T", Content = "C", Tags = new[] { "x" }, Attachments = new Dictionary<string, object>(), Images = images };
        cmd = new CreateGuildPostCommand
        {
            GuildId = cmd.GuildId,
            AuthorAuthUserId = cmd.AuthorAuthUserId,
            Request = request
        };

        storage.SaveImagesAsync(cmd.GuildId, Arg.Any<System.Guid>(), Arg.Any<IEnumerable<(byte[] Content, string ContentType, string FileName)>>()!, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "https://img" });

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.PostId.Should().NotBe(Guid.Empty);
        await postRepo.Received(1).AddAsync(Arg.Any<GuildPost>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RequiresApproval_SetsPendingStatus()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new CreateGuildPostCommandHandler(postRepo, memberRepo, guildRepo, storage);

        var guildId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var request = new CreateGuildPostRequest { Title = "T", Content = "C" };
        var cmd = new CreateGuildPostCommand
        {
            GuildId = guildId,
            AuthorAuthUserId = authorId,
            Request = request
        };

        var guild = new Guild { Id = guildId, RequiresApproval = true };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authorId, Arg.Any<CancellationToken>()).Returns(new GuildMember { Status = MemberStatus.Active });

        await sut.Handle(cmd, CancellationToken.None);
        await postRepo.Received(1).AddAsync(Arg.Is<GuildPost>(p => p.Status == RogueLearn.User.Domain.Enums.GuildPostStatus.pending), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveMembership_Throws()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new CreateGuildPostCommandHandler(postRepo, memberRepo, guildRepo, storage);

        var guildId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var request = new CreateGuildPostRequest { Title = "T", Content = "C" };
        var cmd = new CreateGuildPostCommand
        {
            GuildId = guildId,
            AuthorAuthUserId = authorId,
            Request = request
        };

        var guild = new Guild { Id = guildId, RequiresApproval = false };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authorId, Arg.Any<CancellationToken>()).Returns(new GuildMember { Status = MemberStatus.Inactive });

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MergesExistingImageAttachments()
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var storage = Substitute.For<IGuildPostImageStorage>();
        var sut = new CreateGuildPostCommandHandler(postRepo, memberRepo, guildRepo, storage);

        var guildId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var existingImages = new List<object> { "https://img/old.png" };
        var attachments = new Dictionary<string, object> { ["images"] = existingImages };
        var request = new CreateGuildPostRequest { Title = "T", Content = "C", Attachments = attachments, Images = new List<GuildPostImageUpload> { new(new byte[] { 1 }, "image/png", "new.png") } };
        var cmd = new CreateGuildPostCommand
        {
            GuildId = guildId,
            AuthorAuthUserId = authorId,
            Request = request
        };

        var guild = new Guild { Id = guildId, RequiresApproval = false };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        memberRepo.GetMemberAsync(guildId, authorId, Arg.Any<CancellationToken>()).Returns(new GuildMember { Status = MemberStatus.Active });
        storage.SaveImagesAsync(guildId, Arg.Any<Guid>(), Arg.Any<IEnumerable<(byte[] Content, string ContentType, string FileName)>>()!, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "https://img/new.png" });

        await sut.Handle(cmd, CancellationToken.None);
        await postRepo.Received(1).AddAsync(Arg.Is<GuildPost>(p => ((List<object>)p.Attachments!["images"]).Count == 2), Arg.Any<CancellationToken>());
    }
}
