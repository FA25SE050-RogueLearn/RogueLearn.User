using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.AddSubjectSkillMapping;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.RemoveSubjectSkillMapping;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectById;

namespace RogueLearn.User.Api.Tests.Controllers;

public class SubjectsControllerTests
{
    [Fact]
    public async Task ImportFromText_Returns_BadRequest_If_Empty()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new SubjectsController(mediator);
        var req = new ImportSubjectFromTextRequest { RawText = "   " };
        var res = await controller.ImportFromText(req, CancellationToken.None);
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }



    [Fact]
    public async Task GetAllSubjects_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetAllSubjectsQuery>()).Returns(new PaginatedSubjectsResponse());
        var controller = new SubjectsController(mediator);
        var res = await controller.GetAllSubjects();
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateSubject_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreateSubjectCommand>()).Returns(new CreateSubjectResponse { Id = Guid.NewGuid() });
        var controller = new SubjectsController(mediator);
        var res = await controller.CreateSubject(new CreateSubjectCommand());
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateSubject_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateSubjectCommand>()).Returns(new UpdateSubjectResponse());
        var controller = new SubjectsController(mediator);
        var res = await controller.UpdateSubject(Guid.NewGuid(), new UpdateSubjectCommand());
        res.Result.Should().BeOfType<OkObjectResult>();
    }



    [Fact]
    public async Task GetSubjectById_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSubjectByIdQuery>()).Returns((SubjectDto?)null);
        var controller = new SubjectsController(mediator);
        var res = await controller.GetSubjectById(Guid.NewGuid());
        res.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSubjectById_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSubjectByIdQuery>()).Returns(new SubjectDto());
        var controller = new SubjectsController(mediator);
        var res = await controller.GetSubjectById(Guid.NewGuid());
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteSubject_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<DeleteSubjectCommand>()).Returns(Task.FromResult(Unit.Value));
        var controller = new SubjectsController(mediator);
        var res = await controller.DeleteSubject(Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetSkillMappings_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSubjectSkillMappingsQuery>(), Arg.Any<CancellationToken>()).Returns(new List<SubjectSkillMappingDto>());
        var controller = new SubjectsController(mediator);
        var res = await controller.GetSkillMappings(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddSkillMapping_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<AddSubjectSkillMappingCommand>(), Arg.Any<CancellationToken>()).Returns(new AddSubjectSkillMappingResponse { SubjectId = Guid.NewGuid(), SkillId = Guid.NewGuid() });
        var controller = new SubjectsController(mediator);
        var res = await controller.AddSkillMapping(Guid.NewGuid(), new AddSubjectSkillMappingCommand(), CancellationToken.None);
        res.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task RemoveSkillMapping_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new SubjectsController(mediator);
        var res = await controller.RemoveSkillMapping(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }
}
