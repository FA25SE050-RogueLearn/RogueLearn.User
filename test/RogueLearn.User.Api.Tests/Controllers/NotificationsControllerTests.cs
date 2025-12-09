using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Notifications.Commands.Delete;
using RogueLearn.User.Application.Features.Notifications.Commands.DeleteBatch;
using RogueLearn.User.Application.Features.Notifications.Commands.MarkAllRead;
using RogueLearn.User.Application.Features.Notifications.Commands.MarkAsRead;
using RogueLearn.User.Application.Features.Notifications.Queries.GetMyNotifications;
using RogueLearn.User.Application.Features.Notifications.Queries.GetUnreadCount;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Api.Tests.Controllers;

public class NotificationsControllerTests
{
    private static NotificationsController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new NotificationsController(mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }, "Test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetMyNotifications_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Is<GetMyNotificationsQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>())
                .Returns(new List<Notification>());
        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyNotifications();
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUnreadCount_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetMyUnreadNotificationCountQuery>(), Arg.Any<CancellationToken>())
                .Returns(3);
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetUnreadCount();
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkAsRead_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.MarkAsRead(Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task MarkAllRead_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.MarkAllRead();
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.Delete(Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task BatchDelete_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.BatchDelete(new[] { Guid.NewGuid(), Guid.NewGuid() });
        res.Should().BeOfType<NoContentResult>();
    }
}
