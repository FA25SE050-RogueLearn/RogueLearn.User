using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Notifications.Commands.DeleteBatch;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Notifications.Commands.DeleteBatch;

public class DeleteNotificationsBatchCommandHandlerTests
{
    [Fact]
    public async Task Handle_Empty_List_No_Ops()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new DeleteNotificationsBatchCommandHandler(repo);
        var cmd = new DeleteNotificationsBatchCommand(new List<Guid>(), Guid.NewGuid());
        await sut.Handle(cmd, CancellationToken.None);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new DeleteNotificationsBatchCommandHandler(repo);
        var auth = Guid.NewGuid();
        var ids = new List<Guid> { Guid.NewGuid() };
        var cmd = new DeleteNotificationsBatchCommand(ids, auth);

        repo.GetByIdAsync(ids[0], Arg.Any<CancellationToken>()).Returns((Notification?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Forbidden_When_Not_Owner()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new DeleteNotificationsBatchCommandHandler(repo);
        var auth = Guid.NewGuid();
        var ids = new List<Guid> { Guid.NewGuid() };
        var cmd = new DeleteNotificationsBatchCommand(ids, auth);

        repo.GetByIdAsync(ids[0], Arg.Any<CancellationToken>())
            .Returns(new Notification { Id = ids[0], AuthUserId = Guid.NewGuid() });

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Deletes_Distinct_Ids_When_Owner()
    {
        var repo = Substitute.For<INotificationRepository>();
        var sut = new DeleteNotificationsBatchCommandHandler(repo);
        var auth = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var ids = new List<Guid> { id1, id1, id2 };
        var cmd = new DeleteNotificationsBatchCommand(ids, auth);

        repo.GetByIdAsync(id1, Arg.Any<CancellationToken>()).Returns(new Notification { Id = id1, AuthUserId = auth });
        repo.GetByIdAsync(id2, Arg.Any<CancellationToken>()).Returns(new Notification { Id = id2, AuthUserId = auth });

        await sut.Handle(cmd, CancellationToken.None);

        await repo.Received(1).DeleteAsync(id1, Arg.Any<CancellationToken>());
        await repo.Received(1).DeleteAsync(id2, Arg.Any<CancellationToken>());
        await repo.Received(2).DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

