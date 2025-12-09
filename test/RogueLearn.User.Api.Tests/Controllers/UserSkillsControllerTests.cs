using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkills;

namespace RogueLearn.User.Api.Tests.Controllers;

public class UserSkillsControllerTests
{
    private static UserSkillsController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new UserSkillsController(mediator);
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
    public async Task GetAll_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetUserSkillsQuery>(), Arg.Any<CancellationToken>())
                .Returns(new GetUserSkillsResponse { Skills = new List<UserSkillDto>() });
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.GetAll();
        res.Result.Should().BeOfType<OkObjectResult>();
    }
}
