using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Notifications.Commands.MarkAllRead;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notifications.Commands.MarkAllRead;

public class MarkAllNotificationsReadCommandHandlerTests
{
    [Fact]
    public async Task Handle_No_Unread_Does_Not_Update()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new MarkAllNotificationsReadCommandHandler(repo);
        var auth = Guid.NewGuid();
        var cmd = new MarkAllNotificationsReadCommand(auth);

        repo.GetUnreadByUserAsync(auth, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Notification>());

        await sut.Handle(cmd, CancellationToken.None);
        await repo.DidNotReceive().UpdateRangeAsync(Arg.Any<IEnumerable<Notification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Marks_All_Unread_And_Updates()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new MarkAllNotificationsReadCommandHandler(repo);
        var auth = Guid.NewGuid();
        var cmd = new MarkAllNotificationsReadCommand(auth);

        var n1 = new Notification { Id = Guid.NewGuid(), AuthUserId = auth, IsRead = false };
        var n2 = new Notification { Id = Guid.NewGuid(), AuthUserId = auth, IsRead = false };
        repo.GetUnreadByUserAsync(auth, Arg.Any<CancellationToken>())
            .Returns(new[] { n1, n2 });

        await sut.Handle(cmd, CancellationToken.None);

        Assert.True(n1.IsRead);
        Assert.True(n2.IsRead);
        Assert.NotNull(n1.ReadAt);
        Assert.NotNull(n2.ReadAt);
        await repo.Received(1).UpdateRangeAsync(Arg.Is<IEnumerable<Notification>>(list => list.Count() == 2), Arg.Any<CancellationToken>());
    }
}

