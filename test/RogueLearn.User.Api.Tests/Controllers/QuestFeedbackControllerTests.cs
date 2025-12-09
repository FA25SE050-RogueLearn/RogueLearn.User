using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.QuestFeedback.Commands.ResolveQuestStepFeedback;
using RogueLearn.User.Application.Features.QuestFeedback.Commands.SubmitQuestStepFeedback;
using RogueLearn.User.Application.Features.QuestFeedback.Queries.GetQuestFeedbackList;

namespace RogueLearn.User.Api.Tests.Controllers;

public class QuestFeedbackControllerTests
{
    private static QuestFeedbackController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new QuestFeedbackController(mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }, "Test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task SubmitFeedback_Returns_Created()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<SubmitQuestStepFeedbackCommand>()).Returns(Guid.NewGuid());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.SubmitFeedback(Guid.NewGuid(), Guid.NewGuid(), new SubmitQuestStepFeedbackCommand());
        res.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task AdminGetFeedback_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetQuestFeedbackListQuery>()).Returns(new List<QuestFeedbackDto>());
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.AdminGetFeedback(null, null, true);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResolveFeedback_Returns_BadRequest_On_Id_Mismatch()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = CreateController(mediator, Guid.NewGuid());
        var res = await controller.ResolveFeedback(Guid.NewGuid(), new ResolveQuestStepFeedbackCommand { FeedbackId = Guid.NewGuid() });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResolveFeedback_Returns_NoContent_On_Matching_Id()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ResolveQuestStepFeedbackCommand>()).Returns(Task.FromResult(Unit.Value));
        var controller = CreateController(mediator, Guid.NewGuid());
        var id = Guid.NewGuid();
        var res = await controller.ResolveFeedback(id, new ResolveQuestStepFeedbackCommand { FeedbackId = id });
        res.Should().BeOfType<NoContentResult>();
    }
}
