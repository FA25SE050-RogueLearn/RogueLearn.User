using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestStepContent;

namespace RogueLearn.User.Api.Tests.Controllers;

public class QuestStepContentEditorControllerTests
{
    private static QuestStepContentEditorController CreateController(IMediator mediator)
    {
        var logger = Substitute.For<ILogger<QuestStepContentEditorController>>();
        return new QuestStepContentEditorController(mediator, logger);
    }

    [Fact]
    public async Task GetQuestStepContent_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetQuestStepContentQuery>(), Arg.Any<CancellationToken>()).Returns(new QuestStepContentResponse());
        var controller = CreateController(mediator);
        var res = await controller.GetQuestStepContent(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetQuestStepContent_Returns_500_On_Exception()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetQuestStepContentQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<QuestStepContentResponse>(new Exception("boom")));
        var controller = CreateController(mediator);
        var res = await controller.GetQuestStepContent(Guid.NewGuid(), CancellationToken.None);
        var obj = res as ObjectResult;
        obj.Should().NotBeNull();
        obj!.StatusCode.Should().Be(500);
    }
}
