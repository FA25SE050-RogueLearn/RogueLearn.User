using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

namespace RogueLearn.User.Api.Tests.Controllers;

public class CurriculumProgramsAdminControllerTests
{
    [Fact]
    public async Task GetProgramDetails_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetCurriculumProgramDetailsQuery>(), Arg.Any<CancellationToken>()).Returns(new CurriculumProgramDetailsResponse());
        var controller = new CurriculumProgramsAdminController(mediator);
        var res = await controller.GetProgramDetails(Guid.NewGuid(), CancellationToken.None);
        res.Result.Should().BeOfType<OkObjectResult>();
    }
}

