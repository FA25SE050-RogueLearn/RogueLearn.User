using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.RemoveSpecializationSubject;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;

namespace RogueLearn.User.Api.Tests.Controllers;

public class ClassSpecializationControllerTests
{
    [Fact]
    public async Task GetSpecializationSubjects_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetSpecializationSubjectsQuery>(), Arg.Any<CancellationToken>()).Returns(new List<SpecializationSubjectDto>());
        var controller = new ClassSpecializationController(mediator);
        var res = await controller.GetSpecializationSubjects(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddSpecializationSubject_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<AddSpecializationSubjectCommand>(), Arg.Any<CancellationToken>()).Returns(new SpecializationSubjectDto { ClassId = Guid.NewGuid(), SubjectId = Guid.NewGuid() });
        var controller = new ClassSpecializationController(mediator);
        var res = await controller.AddSpecializationSubject(Guid.NewGuid(), new AddSpecializationSubjectCommand(), CancellationToken.None);
        res.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task RemoveSpecializationSubject_Returns_NoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new ClassSpecializationController(mediator);
        var res = await controller.RemoveSpecializationSubject(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NoContentResult>();
    }
}

