using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.AddSubjectToProgram;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.RemoveSubjectFromProgram;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Queries.GetSubjectsByProgram;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

namespace RogueLearn.User.Api.Tests.Controllers;

public class CurriculumProgramSubjectsControllerTests
{
    [Fact]
    public async Task GetSubjectsByProgram_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSubjectsByProgramQuery>()).Returns(new List<SubjectDto>());
        var controller = new CurriculumProgramSubjectsController(mediator);
        var res = await controller.GetSubjectsByProgram(Guid.NewGuid());
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddSubjectToProgram_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<AddSubjectToProgramCommand>()).Returns(new AddSubjectToProgramResponse { ProgramId = Guid.NewGuid(), SubjectId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow });
        var controller = new CurriculumProgramSubjectsController(mediator);
        var req = new AddSubjectToProgramRequest { SubjectId = Guid.NewGuid() };
        var res = await controller.AddSubjectToProgram(Guid.NewGuid(), req);
        res.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task RemoveSubjectFromProgram_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new CurriculumProgramSubjectsController(mediator);
        var res = await controller.RemoveSubjectFromProgram(Guid.NewGuid(), Guid.NewGuid());
        res.Should().BeOfType<NoContentResult>();
    }
}
