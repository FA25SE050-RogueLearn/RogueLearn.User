using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.Commands.GuildMasterActions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands.GuildMasterActions;

public class GuildMasterActionHandlersTests
{
    [Theory]
    [AutoData]
    public async Task Pin_NotFound_Throws(PinGuildPostCommand cmd)
    {
        var repo = Substitute.For<IGuildPostRepository>();
        repo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns((GuildPost?)null);
        var sut = new PinGuildPostCommandHandler(repo);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Pin_SetsPinnedAndUpdates(PinGuildPostCommand cmd)
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, IsPinned = false };
        repo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        var sut = new PinGuildPostCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => p.IsPinned && p.UpdatedAt <= DateTimeOffset.UtcNow), Arg.Any<CancellationToken>());
    }

    [Theory]
    [AutoData]
    public async Task Unpin_SetsUnpinnedAndUpdates(UnpinGuildPostCommand cmd)
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, IsPinned = true };
        repo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        var sut = new UnpinGuildPostCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => !p.IsPinned && p.UpdatedAt <= DateTimeOffset.UtcNow), Arg.Any<CancellationToken>());
    }

    [Theory]
    [AutoData]
    public async Task Lock_SetsLockedAndUpdates(LockGuildPostCommand cmd)
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, IsLocked = false };
        repo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        var sut = new LockGuildPostCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => p.IsLocked && p.UpdatedAt <= DateTimeOffset.UtcNow), Arg.Any<CancellationToken>());
    }

    [Theory]
    [AutoData]
    public async Task Unlock_SetsUnlockedAndUpdates(UnlockGuildPostCommand cmd)
    {
        var repo = Substitute.For<IGuildPostRepository>();
        var post = new GuildPost { Id = cmd.PostId, GuildId = cmd.GuildId, IsLocked = true };
        repo.GetByIdAsync(cmd.GuildId, cmd.PostId, Arg.Any<CancellationToken>()).Returns(post);
        var sut = new UnlockGuildPostCommandHandler(repo);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<GuildPost>(p => !p.IsLocked && p.UpdatedAt <= DateTimeOffset.UtcNow), Arg.Any<CancellationToken>());
    }
}