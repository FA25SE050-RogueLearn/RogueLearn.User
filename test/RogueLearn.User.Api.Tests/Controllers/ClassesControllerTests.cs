using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;

namespace RogueLearn.User.Api.Tests.Controllers;

public class ClassesControllerTests
{
    [Fact]
    public async Task GetAll_Returns_Ok_With_Classes()
    {
        var mediator = Substitute.For<IMediator>();
        var classes = new List<ClassDto>
        {
            new() { Id = Guid.NewGuid(), Name = "CS", Description = "Computer Science" },
            new() { Id = Guid.NewGuid(), Name = "SE", Description = "Software Engineering" }
        };
        mediator.Send(Arg.Any<GetAllClassesQuery>(), Arg.Any<CancellationToken>())
                .Returns(classes);

        var controller = new ClassesController(mediator);
        var res = await controller.GetAll(CancellationToken.None);

        res.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)res.Result!;
        ok.Value.Should().BeEquivalentTo(classes);
    }
}

