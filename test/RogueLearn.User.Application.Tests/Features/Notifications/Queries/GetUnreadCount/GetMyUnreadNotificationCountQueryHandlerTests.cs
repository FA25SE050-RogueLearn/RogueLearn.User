using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Notifications.Queries.GetUnreadCount;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notifications.Queries.GetUnreadCount;

public class GetMyUnreadNotificationCountQueryHandlerTests
{
    [Fact]
    public async Task Handle_Returns_Count_From_Repository()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new GetMyUnreadNotificationCountQueryHandler(repo);
        var auth = Guid.NewGuid();
        var cmd = new GetMyUnreadNotificationCountQuery(auth);

        repo.CountUnreadByUserAsync(auth, Arg.Any<CancellationToken>()).Returns(7);

        var result = await sut.Handle(cmd, CancellationToken.None);
        Assert.Equal(7, result);
        await repo.Received(1).CountUnreadByUserAsync(auth, Arg.Any<CancellationToken>());
    }
}

