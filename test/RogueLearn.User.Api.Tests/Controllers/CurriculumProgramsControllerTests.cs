using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.CreateCurriculumProgram;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.DeleteCurriculumProgram;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramById;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

namespace RogueLearn.User.Api.Tests.Controllers;

public class CurriculumProgramsControllerTests
{
    [Fact]
    public async Task GetAll_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new CurriculumProgramsController(mediator);
        var res = await controller.GetAll();
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateCurriculumProgramCommand>()).Returns(new CreateCurriculumProgramResponse { Id = Guid.NewGuid() });
        var controller = new CurriculumProgramsController(mediator);
        var res = await controller.Create(new CreateCurriculumProgramCommand());
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateCurriculumProgramCommand>()).Returns(new UpdateCurriculumProgramResponse());
        var controller = new CurriculumProgramsController(mediator);
        var res = await controller.Update(Guid.NewGuid(), new UpdateCurriculumProgramCommand());
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new CurriculumProgramsController(mediator);
        var res = await controller.Delete(Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetById_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetCurriculumProgramByIdQuery>()).Returns(new CurriculumProgramDto());
        var controller = new CurriculumProgramsController(mediator);
        var res = await controller.GetById(Guid.NewGuid());
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetails_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetCurriculumProgramDetailsQuery>()).Returns(new CurriculumProgramDetailsResponse());
        var controller = new CurriculumProgramsController(mediator);
        var res = await controller.GetDetails(Guid.NewGuid());
        res.Result.Should().BeOfType<OkObjectResult>();
    }
}
