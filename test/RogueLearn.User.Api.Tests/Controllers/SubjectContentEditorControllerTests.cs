using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubjectContent;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectContent;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Api.Tests.Controllers;

public class SubjectContentEditorControllerTests
{
    private static SubjectContentEditorController CreateController(IMediator mediator)
    {
        var logger = Substitute.For<ILogger<SubjectContentEditorController>>();
        var controller = new SubjectContentEditorController(mediator, logger);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "Test")) } };
        return controller;
    }

    [Fact]
    public async Task GetSubjectContent_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSubjectContentQuery>(), Arg.Any<CancellationToken>()).Returns(new SyllabusContent());
        var controller = CreateController(mediator);
        var res = await controller.GetSubjectContent(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateSubjectContent_Returns_BadRequest_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator);
        var res = await controller.UpdateSubjectContent(Guid.NewGuid(), null!, CancellationToken.None);
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSubjectContent_Returns_500_On_Exception()
    {
        var mediator = Substitute.For<IMediator>();
        mediator
            .When(m => m.Send(Arg.Any<GetSubjectContentQuery>(), Arg.Any<CancellationToken>()))
            .Do(_ => { throw new Exception("fail"); });
        var controller = CreateController(mediator);
        var res = await controller.GetSubjectContent(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<ObjectResult>();
        ((ObjectResult)res).StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task UpdateSubjectContent_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var content = new SyllabusContent();
        mediator.Send(Arg.Any<UpdateSubjectContentCommand>(), Arg.Any<CancellationToken>()).Returns(content);
        var controller = CreateController(mediator);
        var res = await controller.UpdateSubjectContent(Guid.NewGuid(), content, CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateSubjectContent_Returns_500_On_Exception()
    {
        var mediator = Substitute.For<IMediator>();
        mediator
            .When(m => m.Send(Arg.Any<UpdateSubjectContentCommand>(), Arg.Any<CancellationToken>()))
            .Do(_ => { throw new Exception("fail"); });
        var controller = CreateController(mediator);
        var res = await controller.UpdateSubjectContent(Guid.NewGuid(), new SyllabusContent(), CancellationToken.None);
        res.Should().BeOfType<ObjectResult>();
        ((ObjectResult)res).StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
