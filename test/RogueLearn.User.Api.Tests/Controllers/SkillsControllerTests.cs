using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillTree;

namespace RogueLearn.User.Api.Tests.Controllers;

public class SkillsControllerTests
{
    private static SkillsController CreateController(IMediator mediator)
    {
        var controller = new SkillsController(mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "Test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetSkillTree_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSkillTreeQuery>(), Arg.Any<CancellationToken>()).Returns(new SkillTreeDto());
        var controller = CreateController(mediator);
        var res = await controller.GetSkillTree(CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSkillDetail_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSkillDetailQuery>(), Arg.Any<CancellationToken>()).Returns((SkillDetailDto?)null);
        var controller = CreateController(mediator);
        var res = await controller.GetSkillDetail(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<NotFoundResult>();
    }
}

