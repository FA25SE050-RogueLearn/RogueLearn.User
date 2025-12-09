using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetStepProgress;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestProgress;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetCompletedActivities;

namespace RogueLearn.User.Api.Tests.Controllers;

public class UserQuestProgressControllerTests
{
    private static UserQuestProgressController CreateController(IMediator mediator)
    {
        var logger = Substitute.For<ILogger<UserQuestProgressController>>();
        var controller = new UserQuestProgressController(mediator, logger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                }, "Test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetUserQuestProgress_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetQuestProgressQuery>(), Arg.Any<CancellationToken>()).Returns(new List<QuestStepProgressDto>());
        var controller = CreateController(mediator);
        var res = await controller.GetUserQuestProgress(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(res);
    }

    [Fact]
    public async Task GetUserQuestProgress_Returns_NotFound_On_NotFoundException()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetQuestProgressQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<QuestStepProgressDto>>(new RogueLearn.User.Application.Exceptions.NotFoundException("missing")));
        var controller = CreateController(mediator);
        var res = await controller.GetUserQuestProgress(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUserQuestProgress_Returns_500_On_Exception()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetQuestProgressQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<QuestStepProgressDto>>(new Exception("boom")));
        var controller = CreateController(mediator);
        var res = await controller.GetUserQuestProgress(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<ObjectResult>();
        ((ObjectResult)res).StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task GetStepProgress_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetStepProgressQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetStepProgressResponse()));
        var controller = CreateController(mediator);
        var res = await controller.GetStepProgress(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(res);
    }

    [Fact]
    public async Task GetStepProgress_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetStepProgressQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GetStepProgressResponse?>(null)!);
        var controller = CreateController(mediator);
        var res = await controller.GetStepProgress(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetStepProgress_Returns_500_On_Exception()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetStepProgressQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetStepProgressResponse>(new Exception("err")));
        var controller = CreateController(mediator);
        var res = await controller.GetStepProgress(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<ObjectResult>();
        ((ObjectResult)res).StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task GetStepActivities_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetCompletedActivitiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CompletedActivitiesDto()));
        var controller = CreateController(mediator);
        var res = await controller.GetStepActivities(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(res);
    }

    [Fact]
    public async Task GetStepActivities_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetCompletedActivitiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CompletedActivitiesDto?>(null)!);
        var controller = CreateController(mediator);
        var res = await controller.GetStepActivities(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetStepActivities_Returns_500_On_Exception()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetCompletedActivitiesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CompletedActivitiesDto>(new Exception("x")));
        var controller = CreateController(mediator);
        var res = await controller.GetStepActivities(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<ObjectResult>();
        ((ObjectResult)res).StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
