using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;

namespace RogueLearn.User.Api.Tests.Controllers;

public class CurriculumImportControllerTests
{
    [Fact]
    public async Task ImportCurriculum_Returns_BadRequest_When_Empty_Text()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new CurriculumImportController(mediator);

        var res = await controller.ImportCurriculum("");

        res.Should().BeOfType<BadRequestObjectResult>();
        var bad = (BadRequestObjectResult)res;
        bad.Value.Should().Be("Raw text is required");
    }

    [Fact]
    public async Task ImportCurriculum_Returns_Ok_On_Success()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ImportCurriculumCommand>()).Returns(new ImportCurriculumResponse { IsSuccess = true, Message = "ok" });
        var controller = new CurriculumImportController(mediator);

        var res = await controller.ImportCurriculum("text");

        res.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)res;
        ok.Value.Should().BeOfType<ImportCurriculumResponse>();
        ((ImportCurriculumResponse)ok.Value!).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ImportCurriculum_Returns_BadRequest_On_Failure()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ImportCurriculumCommand>()).Returns(new ImportCurriculumResponse { IsSuccess = false, Message = "err" });
        var controller = new CurriculumImportController(mediator);

        var res = await controller.ImportCurriculum("text");

        res.Should().BeOfType<BadRequestObjectResult>();
        var bad = (BadRequestObjectResult)res;
        bad.Value.Should().BeOfType<ImportCurriculumResponse>();
        ((ImportCurriculumResponse)bad.Value!).IsSuccess.Should().BeFalse();
    }
}

