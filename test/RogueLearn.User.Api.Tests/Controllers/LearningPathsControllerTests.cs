using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

namespace RogueLearn.User.Api.Tests.Controllers;

public class LearningPathsControllerTests
{
    private static LearningPathsController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new LearningPathsController(mediator);
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
    public async Task GetMyLearningPath_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        var expected = new LearningPathDto { Id = Guid.NewGuid(), Name = "LP" };

        mediator.Send(Arg.Is<GetMyLearningPathQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>())
                .Returns(expected);

        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyLearningPath(CancellationToken.None);

        res.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)res.Result!;
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetMyLearningPath_Returns_NotFound_When_Missing()
    {
        var mediator = Substitute.For<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Is<GetMyLearningPathQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>())
                .Returns((LearningPathDto?)null);

        var controller = CreateController(mediator, userId);
        var res = await controller.GetMyLearningPath(CancellationToken.None);

        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteLearningPath_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<RogueLearn.User.Application.Features.LearningPaths.Commands.DeleteLearningPath.DeleteLearningPathCommand>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Unit.Value));
        var controller = new LearningPathsController(mediator);
        var res = await controller.DeleteLearningPath(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }
}
