using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;
using RogueLearn.User.Application.Features.Quests.Commands.StartQuest;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Application.Features.Quests.Queries.GetMyQuestsWithSubjects;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestSkills;
using RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitQuizAnswer;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Api.Tests.Controllers;

public class QuestsControllerTests
{
    private static QuestsController CreateController(IMediator mediator, IQuestRepository questRepo, IQuestStepRepository stepRepo, Guid userId)
    {
        var logger = Substitute.For<ILogger<QuestsController>>();
        var controller = new QuestsController(mediator, questRepo, stepRepo, logger);
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
    public async Task GetQuestById_Returns_Ok_When_Found()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var userId = Guid.NewGuid();
        var expected = new QuestDetailsDto();
        mediator.Send(Arg.Is<GetQuestByIdQuery>(q => q.AuthUserId == userId), Arg.Any<CancellationToken>())
                .Returns(expected);
        var controller = CreateController(mediator, questRepo, stepRepo, userId);
        var res = await controller.GetQuestById(Guid.NewGuid());
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetQuestById_Returns_NotFound_When_Missing()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<GetQuestByIdQuery>(), Arg.Any<CancellationToken>())!.Returns((QuestDetailsDto?)null);
        var controller = CreateController(mediator, questRepo, stepRepo, userId);
        var res = await controller.GetQuestById(Guid.NewGuid());
        res.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task StartQuest_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var userId = Guid.NewGuid();
        mediator.Send(Arg.Any<StartQuestCommand>(), Arg.Any<CancellationToken>()).Returns(new StartQuestResponse());
        var controller = CreateController(mediator, questRepo, stepRepo, userId);
        var res = await controller.StartQuest(Guid.NewGuid());
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateQuestActivityProgress_Returns_NoContent_On_Success()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var controller = CreateController(mediator, questRepo, stepRepo, Guid.NewGuid());
        var res = await controller.UpdateQuestActivityProgress(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new UpdateQuestActivityProgressRequest { Status = RogueLearn.User.Domain.Enums.StepCompletionStatus.Completed });
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateQuestActivityProgress_Returns_BadRequest_On_Validation()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateQuestActivityProgressCommand>()).Returns(Task.FromException(new ValidationException("invalid")));
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var controller = CreateController(mediator, questRepo, stepRepo, Guid.NewGuid());
        var res = await controller.UpdateQuestActivityProgress(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new UpdateQuestActivityProgressRequest { Status = RogueLearn.User.Domain.Enums.StepCompletionStatus.InProgress });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitQuizAnswer_Returns_NotFound_When_Quest_Missing()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        questRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Quest?)null);
        var controller = CreateController(mediator, questRepo, stepRepo, Guid.NewGuid());
        var res = await controller.SubmitQuizAnswer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new SubmitQuizAnswerRequest());
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMyQuestsWithSubjects_Returns_Ok()
    {
        var mediator = Substitute.For<IMediator>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var controller = CreateController(mediator, questRepo, stepRepo, Guid.NewGuid());
        var res = await controller.GetMyQuestsWithSubjects();
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetQuestSkills_Returns_NotFound_When_Null()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetQuestSkillsQuery>(), Arg.Any<CancellationToken>()).Returns((GetQuestSkillsResponse?)null);
        var controller = CreateController(mediator, Substitute.For<IQuestRepository>(), Substitute.For<IQuestStepRepository>(), Guid.NewGuid());
        var res = await controller.GetQuestSkills(Guid.NewGuid(), CancellationToken.None);
        res.Should().BeOfType<NotFoundResult>();
    }
}

