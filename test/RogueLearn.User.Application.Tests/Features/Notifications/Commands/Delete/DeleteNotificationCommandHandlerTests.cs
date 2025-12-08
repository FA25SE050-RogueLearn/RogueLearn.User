using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notifications.Commands.Delete;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notifications.Commands.Delete;

public class DeleteNotificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new DeleteNotificationCommandHandler(repo);
        var cmd = new DeleteNotificationCommand(Guid.NewGuid(), Guid.NewGuid());
        repo.GetByIdAsync(cmd.NotificationId, Arg.Any<CancellationToken>()).Returns((Notification?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Forbidden_When_Not_Owner()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new DeleteNotificationCommandHandler(repo);
        var owner = Guid.NewGuid();
        var cmd = new DeleteNotificationCommand(Guid.NewGuid(), Guid.NewGuid());
        repo.GetByIdAsync(cmd.NotificationId, Arg.Any<CancellationToken>())
            .Returns(new Notification { Id = cmd.NotificationId, AuthUserId = owner });

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Deletes_When_Owner()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new DeleteNotificationCommandHandler(repo);
        var auth = Guid.NewGuid();
        var id = Guid.NewGuid();
        var cmd = new DeleteNotificationCommand(id, auth);
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Notification { Id = id, AuthUserId = auth });

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}

