using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notifications.Commands.MarkAsRead;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notifications.Commands.MarkAsRead;

public class MarkNotificationReadCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new MarkNotificationReadCommandHandler(repo);
        var cmd = new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid());
        repo.GetByIdAsync(cmd.NotificationId, Arg.Any<CancellationToken>()).Returns((Notification?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Forbidden_When_Not_Owner()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new MarkNotificationReadCommandHandler(repo);
        var owner = Guid.NewGuid();
        var cmd = new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid());
        repo.GetByIdAsync(cmd.NotificationId, Arg.Any<CancellationToken>())
            .Returns(new Notification { Id = cmd.NotificationId, AuthUserId = owner });

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Already_Read_Does_Not_Update()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new MarkNotificationReadCommandHandler(repo);
        var auth = Guid.NewGuid();
        var id = Guid.NewGuid();
        var cmd = new MarkNotificationReadCommand(id, auth);
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Notification { Id = id, AuthUserId = auth, IsRead = true, ReadAt = DateTimeOffset.UtcNow });

        await sut.Handle(cmd, CancellationToken.None);
        await repo.DidNotReceive().UpdateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Marks_Read_And_Updates_When_Unread()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new MarkNotificationReadCommandHandler(repo);
        var auth = Guid.NewGuid();
        var id = Guid.NewGuid();
        var cmd = new MarkNotificationReadCommand(id, auth);
        var entity = new Notification { Id = id, AuthUserId = auth, IsRead = false };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(entity);

        await sut.Handle(cmd, CancellationToken.None);

        Assert.True(entity.IsRead);
        Assert.NotNull(entity.ReadAt);
        await repo.Received(1).UpdateAsync(entity, Arg.Any<CancellationToken>());
    }
}

