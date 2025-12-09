using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;

namespace RogueLearn.User.Api.Tests.Controllers;

public class UserAchievementsControllerTests
{
    private static UserAchievementsController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new UserAchievementsController(mediator);
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
    public async Task GetMyAchievements_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetUserAchievementsByAuthIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(new GetUserAchievementsByAuthIdResponse { Achievements = new List<UserAchievementDto>() });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetMyAchievements();
        res.Result.Should().BeOfType<OkObjectResult>();
    }
}
