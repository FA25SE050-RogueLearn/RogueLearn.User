using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Likes.LikeGuildPost;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.Likes.LikeGuildPost;

public class LikeGuildPostCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotMember_Throws(LikeGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var likeRepo = Substitute.For<IGuildPostLikeRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var sut = new LikeGuildPostCommandHandler(postRepo, likeRepo, memberRepo, notificationRepo);

        memberRepo.GetMemberAsync(cmd.GuildId, cmd.UserId, Arg.Any<CancellationToken>()).Returns((GuildMember?)null);
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_AlreadyLiked_NoDuplicate(LikeGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var likeRepo = Substitute.For<IGuildPostLikeRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var sut = new LikeGuildPostCommandHandler(postRepo, likeRepo, memberRepo, notificationRepo);

        memberRepo.GetMemberAsync(cmd.GuildId, cmd.UserId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        var post = new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId };
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        likeRepo.GetByPostAndUserAsync(cmd.PostId, cmd.UserId, Arg.Any<CancellationToken>()).Returns(new GuildPostLike { Id = System.Guid.NewGuid(), PostId = cmd.PostId, UserId = cmd.UserId });

        await sut.Handle(cmd, CancellationToken.None);
        await likeRepo.DidNotReceive().AddAsync(Arg.Any<GuildPostLike>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [AutoData]
    public async Task Handle_Locked_Throws(LikeGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var likeRepo = Substitute.For<IGuildPostLikeRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var sut = new LikeGuildPostCommandHandler(postRepo, likeRepo, memberRepo, notificationRepo);

        memberRepo.GetMemberAsync(cmd.GuildId, cmd.UserId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId, IsLocked = true });
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_AddsLikeAndUpdatesCount(LikeGuildPostCommand cmd)
    {
        var postRepo = Substitute.For<IGuildPostRepository>();
        var likeRepo = Substitute.For<IGuildPostLikeRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var sut = new LikeGuildPostCommandHandler(postRepo, likeRepo, memberRepo, notificationRepo);

        var post = new GuildPost { GuildId = cmd.GuildId, Id = cmd.PostId, AuthorId = Guid.NewGuid(), LikeCount = 0, IsLocked = false };
        memberRepo.GetMemberAsync(cmd.GuildId, cmd.UserId, Arg.Any<CancellationToken>()).Returns(new GuildMember());
        postRepo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        likeRepo.GetByPostAndUserAsync(cmd.PostId, cmd.UserId, Arg.Any<CancellationToken>()).Returns((GuildPostLike?)null);

        await sut.Handle(cmd, CancellationToken.None);
        await likeRepo.Received(1).AddAsync(Arg.Any<GuildPostLike>(), Arg.Any<CancellationToken>());
        await postRepo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => p.LikeCount == 1), Arg.Any<CancellationToken>());
        await notificationRepo.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }
}