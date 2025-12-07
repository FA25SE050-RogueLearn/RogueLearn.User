using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Notifications.Queries.GetMyNotifications;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Defaults_Size_To_20_When_Non_Positive()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new GetMyNotificationsQueryHandler(repo);
        var auth = Guid.NewGuid();
        var cmd = new GetMyNotificationsQuery(auth, 0);

        var items = new[] { new Notification { Id = Guid.NewGuid(), AuthUserId = auth } };
        repo.GetLatestByUserAsync(auth, 20, Arg.Any<CancellationToken>()).Returns(items);

        var result = await sut.Handle(cmd, CancellationToken.None);
        Assert.Single(result);
        await repo.Received(1).GetLatestByUserAsync(auth, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Caps_Size_At_100()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new GetMyNotificationsQueryHandler(repo);
        var auth = Guid.NewGuid();
        var cmd = new GetMyNotificationsQuery(auth, 1000);

        var items = Enumerable.Range(0, 5).Select(_ => new Notification { Id = Guid.NewGuid(), AuthUserId = auth }).ToList();
        repo.GetLatestByUserAsync(auth, 100, Arg.Any<CancellationToken>()).Returns(items);

        var result = await sut.Handle(cmd, CancellationToken.None);
        Assert.Equal(5, result.Count);
        await repo.Received(1).GetLatestByUserAsync(auth, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Uses_Requested_Size_When_Valid()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new GetMyNotificationsQueryHandler(repo);
        var auth = Guid.NewGuid();
        var cmd = new GetMyNotificationsQuery(auth, 42);

        var items = new List<Notification> { new Notification { Id = Guid.NewGuid(), AuthUserId = auth } };
        repo.GetLatestByUserAsync(auth, 42, Arg.Any<CancellationToken>()).Returns(items);

        var result = await sut.Handle(cmd, CancellationToken.None);
        Assert.Equal(items.Count, result.Count);
        await repo.Received(1).GetLatestByUserAsync(auth, 42, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmpty()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new GetMyNotificationsQueryHandler(repo);
        var auth = Guid.NewGuid();
        var cmd = new GetMyNotificationsQuery(auth, 10);
        repo.GetLatestByUserAsync(auth, 10, Arg.Any<CancellationToken>()).Returns(new List<Notification>());
        var result = await sut.Handle(cmd, CancellationToken.None);
        Assert.Empty(result);
    }
}
