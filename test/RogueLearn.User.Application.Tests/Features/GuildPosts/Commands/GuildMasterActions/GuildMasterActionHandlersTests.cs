using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.GuildMasterActions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.GuildMasterActions;

public class GuildMasterActionHandlersTests
{
    [Fact]
    public async Task Pin_NotFound_Throws()
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var cmd = new PinGuildPostCommand(guildId, postId);
        repo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns((GuildPost?)null);
        var sut = new PinGuildPostCommandHandler(repo);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Pin_SetsPinnedAndUpdates()
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var cmd = new PinGuildPostCommand(guildId, postId);
        var post = new GuildPost { Id = postId, GuildId = guildId, IsPinned = false };
        repo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        var sut = new PinGuildPostCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => p.IsPinned && p.UpdatedAt <= DateTimeOffset.UtcNow), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unpin_SetsUnpinnedAndUpdates()
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var cmd = new UnpinGuildPostCommand(guildId, postId);
        var post = new GuildPost { Id = postId, GuildId = guildId, IsPinned = true };
        repo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        var sut = new UnpinGuildPostCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => !p.IsPinned && p.UpdatedAt <= DateTimeOffset.UtcNow), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lock_SetsLockedAndUpdates()
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var cmd = new LockGuildPostCommand(guildId, postId);
        var post = new GuildPost { Id = postId, GuildId = guildId, IsLocked = false };
        repo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        var sut = new LockGuildPostCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => p.IsLocked && p.UpdatedAt <= DateTimeOffset.UtcNow), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unlock_SetsUnlockedAndUpdates()
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var guildId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();
        var cmd = new UnlockGuildPostCommand(guildId, postId);
        var post = new GuildPost { Id = postId, GuildId = guildId, IsLocked = true };
        repo.GetByIdAsync(guildId, postId, Arg.Any<CancellationToken>()).Returns(post);
        var sut = new UnlockGuildPostCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => !p.IsLocked && p.UpdatedAt <= DateTimeOffset.UtcNow), Arg.Any<CancellationToken>());
    }
}