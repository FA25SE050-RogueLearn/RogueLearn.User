using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.CreateGuildPostComment;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.Comments.CreateGuildPostComment;

public class CreateGuildPostCommentCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_Unauthorized_WhenNotMember(CreateGuildPostCommentCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var sut = new CreateGuildPostCommentCommandHandler(postRepo, commentRepo, memberRepo, notificationRepo);

        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthorId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_PostLocked_Throws(CreateGuildPostCommentCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var sut = new CreateGuildPostCommentCommandHandler(postRepo, commentRepo, memberRepo, notificationRepo);

        memberRepo.GetMemberAsync(cmd.GuildId, cmd.AuthorId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId, IsLocked = true });
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidParentComment_Throws()
    {
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var request = new CreateGuildPostCommentRequest { Content = "c", ParentCommentId = Guid.NewGuid() };
        var cmd = new CreateGuildPostCommentCommand(guildId, postId, authorId, request);

        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var sut = new CreateGuildPostCommentCommandHandler(postRepo, commentRepo, memberRepo, notificationRepo);

        memberRepo.GetMemberAsync(guildId, authorId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(new GuildPost { GuildId = guildId, Id = postId, IsLocked = false });
        commentRepo.GetByIdAsync(request.ParentCommentId.Value, Arg.Any<CancellationToken>()).Returns(new GuildPostComment { Id = Guid.NewGuid(), PostId = Guid.NewGuid() });

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_IncrementsCount_And_NotifiesIfDifferentAuthor()
    {
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var request = new CreateGuildPostCommentRequest { Content = "c" };
        var cmd = new CreateGuildPostCommentCommand(guildId, postId, authorId, request);

        var postRepo = Substitute.For<IGuildPostRepository>();
        var commentRepo = Substitute.For<IGuildPostCommentRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var sut = new CreateGuildPostCommentCommandHandler(postRepo, commentRepo, memberRepo, notificationRepo);

        var post = new GuildPost { GuildId = guildId, Id = postId, AuthorId = Guid.NewGuid(), CommentCount = 0, IsLocked = false };
        memberRepo.GetMemberAsync(guildId, authorId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        postRepo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        commentRepo.AddAsync(Arg.Any<GuildPostComment>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildPostComment>());

        var res = await sut.Handle(cmd, CancellationToken.None);
        await postRepo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => p.CommentCount == 1), Arg.Any<CancellationToken>());
        await notificationRepo.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        Assert.NotEqual(Guid.Empty, res.CommentId);
    }
}