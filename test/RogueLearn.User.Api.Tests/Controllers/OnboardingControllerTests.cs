using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Onboarding.Commands.CompleteOnboarding;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Api.Tests.Controllers;

public class OnboardingControllerTests
{
    private static OnboardingController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new OnboardingController(mediator);
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
    public async Task GetAllRoutes_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAllRoutesQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<RouteDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetAllRoutes(CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllClasses_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAllClassesQuery>(), Arg.Any<CancellationToken>())
                .Returns(new List<ClassDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetAllClasses(CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CompleteOnboarding_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.CompleteOnboarding(new CompleteOnboardingCommand(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }
}
