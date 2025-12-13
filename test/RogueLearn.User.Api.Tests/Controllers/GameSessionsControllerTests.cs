using System.Text;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Api.Controllers;
using RogueLearn.User.Application.Features.GameSessions.Commands.CompleteGameSession;
using RogueLearn.User.Application.Features.UnityMatches.Commands.SubmitUnityMatchResult;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Api.Tests.Controllers;

public class GameSessionsControllerTests
{
    private static GameSessionsController CreateController(IMediator mediator)
    {
        var logger = Substitute.For<ILogger<GameSessionsController>>();
        var controller = new GameSessionsController(mediator, logger);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    // [Fact]
    // public async Task CreateSession_Returns_Created_And_Saves()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     subjectRepo.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);
    //     questStepRepo.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<QuestStep>());
    //     gameSessionRepo.AddAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GameSession>());

    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var req = new GameSessionsController.CreateSessionRequest
    //     {
    //         RelayJoinCode = "ABCDEF",
    //         PackSpec = null,
    //         UserId = null
    //     };

    //     var res = await controller.CreateSession(req);
    //     res.Should().BeOfType<CreatedResult>();
    //     var created = (CreatedResult)res;
    //     created.Value.Should().NotBeNull();
    //     var obj = JsonSerializer.Serialize(created.Value);
    //     obj.Should().Contain("match_id");
    //     obj.Should().Contain("pack_url");
    //     await gameSessionRepo.Received(1).AddAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>());
    // }

    // [Fact]
    // public async Task CompleteSession_Returns_Created_When_NotCompleted()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     var sessionId = Guid.NewGuid();
    //     mediator.Send(Arg.Is<CompleteGameSessionCommand>(c => c.SessionId == sessionId), Arg.Any<CancellationToken>())
    //         .Returns(new CompleteGameSessionResponse { MatchId = sessionId.ToString(), AlreadyCompleted = false });

    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = await controller.CompleteSession(sessionId, JsonDocument.Parse("{}").RootElement);
    //     res.Should().BeOfType<CreatedResult>();
    //     var created = (CreatedResult)res;
    //     var payload = JsonSerializer.Serialize(created.Value);
    //     payload.Should().Contain(sessionId.ToString());
    // }

    // [Fact]
    // public async Task CompleteSession_Returns_Ok_When_AlreadyCompleted()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     var sessionId = Guid.NewGuid();
    //     mediator.Send(Arg.Is<CompleteGameSessionCommand>(c => c.SessionId == sessionId), Arg.Any<CancellationToken>())
    //         .Returns(new CompleteGameSessionResponse { MatchId = sessionId.ToString(), AlreadyCompleted = true });

    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = await controller.CompleteSession(sessionId, JsonDocument.Parse("{}").RootElement);
    //     res.Should().BeOfType<OkObjectResult>();
    //     var ok = (OkObjectResult)res;
    //     var payload = JsonSerializer.Serialize(ok.Value);
    //     payload.Should().Contain("already_completed");
    // }

    // [Fact]
    // public async Task GetPack_Returns_NotFound_When_Missing()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     gameSessionRepo.GetBySessionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((GameSession?)null);
    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = await controller.GetPack(Guid.NewGuid());
    //     res.Should().BeOfType<NotFoundObjectResult>();
    // }

    // [Fact]
    // public async Task GetPack_Returns_Content_When_Found()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     var sessionId = Guid.NewGuid();
    //     var packJson = "{\"packId\":\"p\",\"questions\":[]}";
    //     gameSessionRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
    //         .Returns(new GameSession { SessionId = sessionId, QuestionPackJson = packJson });

    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = await controller.GetPack(sessionId);
    //     res.Should().BeOfType<ContentResult>();
    //     var content = (ContentResult)res;
    //     content.ContentType.Should().Be("application/json");
    //     content.Content.Should().Be(packJson);
    // }

    // [Fact]
    // public async Task GetPlayers_Returns_Ok_List()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     var sessionId = Guid.NewGuid();
    //     matchPlayerSummaryRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
    //         .Returns(new List<MatchPlayerSummary>
    //         {
    //             new MatchPlayerSummary
    //             {
    //                 Id = Guid.NewGuid(),
    //                 SessionId = sessionId,
    //                 UserId = Guid.NewGuid(),
    //                 ClientId = null,
    //                 TotalQuestions = 5,
    //                 CorrectAnswers = 3,
    //                 AverageTime = 1.5,
    //                 TopicBreakdownJson = null,
    //                 CreatedAt = DateTimeOffset.UtcNow
    //             }
    //         });

    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = await controller.GetPlayers(sessionId);
    //     res.Should().BeOfType<OkObjectResult>();
    //     var ok = (OkObjectResult)res;
    //     ok.Value.Should().BeAssignableTo<IEnumerable<object>>();
    // }

    // [Fact]
    // public async Task ResolveByJoinCode_Returns_Ok_When_Found()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     var sessionId = Guid.NewGuid();
    //     gameSessionRepo.GetByJoinCodeAsync("ABCDEF", Arg.Any<CancellationToken>())
    //         .Returns(new GameSession { SessionId = sessionId });

    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = await controller.ResolveByJoinCode("ABCDEF");
    //     res.Should().BeOfType<OkObjectResult>();
    //     var ok = (OkObjectResult)res;
    //     var payload = JsonSerializer.Serialize(ok.Value);
    //     payload.Should().Contain(sessionId.ToString());
    // }

    // [Fact]
    // public async Task ResolveByJoinCode_Returns_BadRequest_When_Missing()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = await controller.ResolveByJoinCode("");
    //     res.Should().BeOfType<BadRequestObjectResult>();
    // }

    // [Fact]
    // public async Task SubmitUnityMatchResult_Returns_Ok()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     mediator.Send(Arg.Any<SubmitUnityMatchResultCommand>(), Arg.Any<CancellationToken>())
    //         .Returns(new SubmitUnityMatchResultResponse { Success = true, MatchId = "m1", SessionId = null });

    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var json = "{\"matchId\":\"m1\",\"result\":\"win\",\"totalPlayers\":2}";
    //     var bytes = Encoding.UTF8.GetBytes(json);
    //     controller.ControllerContext.HttpContext.Request.Body = new MemoryStream(bytes);

    //     var res = await controller.SubmitUnityMatchResult();
    //     res.Should().BeOfType<OkObjectResult>();
    //     var ok = (OkObjectResult)res;
    //     var payload = JsonSerializer.Serialize(ok.Value);
    //     payload.Should().Contain("m1");
    // }

    // [Fact]
    // public async Task GetUnityMatches_Returns_Content()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     matchResultRepo.GetRecentMatchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
    //         .Returns(new List<MatchResult> { new MatchResult { MatchDataJson = "{}" } });

    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = await controller.GetUnityMatches(5, null);
    //     res.Should().BeOfType<ContentResult>();
    //     var content = (ContentResult)res;
    //     content.Content.Should().StartWith("{\"matches\":");
    // }

    // [Fact]
    // public void GetUnityMatch_Returns_NotFound_When_DirMissing()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     Environment.SetEnvironmentVariable("RESULTS_LOG_ROOT", Guid.NewGuid().ToString());
    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = controller.GetUnityMatch("abc");
    //     res.Should().BeOfType<NotFoundObjectResult>();
    // }

    // [Fact]
    // public void GetLastSummary_Returns_NotFound_When_NoDir()
    // {
    //     var mediator = Substitute.For<IMediator>();
    //     var subjectRepo = Substitute.For<ISubjectRepository>();
    //     var matchResultRepo = Substitute.For<IMatchResultRepository>();
    //     var gameSessionRepo = Substitute.For<IGameSessionRepository>();
    //     var matchPlayerSummaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    //     var questStepRepo = Substitute.For<IQuestStepRepository>();

    //     Environment.SetEnvironmentVariable("RESULTS_DIR", Guid.NewGuid().ToString());
    //     var controller = CreateController(mediator, subjectRepo, matchResultRepo, gameSessionRepo, matchPlayerSummaryRepo, questStepRepo);
    //     var res = controller.GetLastSummary("user1");
    //     res.Should().BeOfType<NotFoundObjectResult>();
    // }
}

