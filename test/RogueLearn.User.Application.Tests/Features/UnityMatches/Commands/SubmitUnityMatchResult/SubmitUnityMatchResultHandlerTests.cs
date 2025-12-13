using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.UnityMatches.Commands.SubmitUnityMatchResult;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.UnityMatches.Commands.SubmitUnityMatchResult;

public class SubmitUnityMatchResultHandlerTests
{
    private static SubmitUnityMatchResultCommand BuildCommand(Guid sessionId, Guid userId, int players = 1)
    {
        var raw = JsonSerializer.Serialize(new
        {
            sessionId = sessionId.ToString(),
            playerSummaries = new[]
            {
                new { playerId = 123L, userId = userId.ToString(), totalQuestions = 3, correctAnswers = 2, averageTime = 1.5, topicBreakdown = new[]{ new { total = 3, correct = 2 } } }
            }
        });

        return new SubmitUnityMatchResultCommand
        {
            MatchId = sessionId.ToString(),
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = players,
            UserId = userId,
            RawJson = raw
        };
    }

    private static SubmitUnityMatchResultHandler CreateSut(
        IMatchResultRepository mrRepo,
        IGameSessionRepository gsRepo,
        IMatchPlayerSummaryRepository sumRepo,
        Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler> logger)
    {
        var skillRepo = NSubstitute.Substitute.For<ISkillRepository>();
        skillRepo.GetAllAsync(NSubstitute.Arg.Any<System.Threading.CancellationToken>())
            .Returns(System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IEnumerable<Skill>>(new System.Collections.Generic.List<Skill>()));

        var mediator = NSubstitute.Substitute.For<MediatR.IMediator>();
        mediator.Send(NSubstitute.Arg.Any<object>(), NSubstitute.Arg.Any<System.Threading.CancellationToken>())
            .Returns(System.Threading.Tasks.Task.FromResult<object>(new object()));

        return new SubmitUnityMatchResultHandler(mrRepo, gsRepo, sumRepo, skillRepo, mediator, logger);
    }

    [Fact(Skip="Disabled per request")]
    public void FromPayload_ParsesBasicFields_WithDefaultsWhenMissing()
    {
        var start = DateTime.UtcNow.AddMinutes(-7);
        var end = DateTime.UtcNow.AddMinutes(-2);
        var userId = Guid.NewGuid();
        var payload = JsonSerializer.SerializeToElement(new
        {
            matchId = "m-1",
            result = "win",
            joinCode = "ABCDEF",
            scene = "arena",
            startUtc = start.ToString("O"),
            endUtc = end.ToString("O"),
            userId = userId.ToString(),
            totalPlayers = 3
        });

        var cmd = SubmitUnityMatchResultCommand.FromPayload(payload);

        cmd.MatchId.Should().Be("m-1");
        cmd.Result.Should().Be("win");
        cmd.JoinCode.Should().Be("ABCDEF");
        cmd.Scene.Should().Be("arena");
        cmd.StartUtc.ToUniversalTime().Should().Be(start);
        cmd.EndUtc.ToUniversalTime().Should().Be(end);
        cmd.TotalPlayers.Should().Be(3);
        cmd.UserId.Should().Be(userId);
        cmd.RawJson.Should().Contain("\"matchId\":\"m-1\"");
    }

    [Fact(Skip="Disabled per request")]
    public void FromPayload_TotalPlayers_FromPerPlayerArray()
    {
        var payload = JsonSerializer.SerializeToElement(new
        {
            per_player = new[] { new { id = 1 }, new { id = 2 }, new { id = 3 }, new { id = 4 } }
        });
        var cmd = SubmitUnityMatchResultCommand.FromPayload(payload);
        cmd.TotalPlayers.Should().Be(4);
    }

    [Fact(Skip="Disabled per request")]
    public void FromPayload_TotalPlayers_FromPlayerSummariesArray()
    {
        var payload = JsonSerializer.SerializeToElement(new
        {
            playerSummaries = new[] { new { id = 1 }, new { id = 2 } }
        });
        var cmd = SubmitUnityMatchResultCommand.FromPayload(payload);
        cmd.TotalPlayers.Should().Be(2);
    }

    [Fact(Skip="Disabled per request")]
    public void FromPayload_NotObject_UsesFallbacks()
    {
        var payload = JsonSerializer.SerializeToElement(new[] { 1, 2, 3 });
        var before = DateTime.UtcNow;
        var cmd = SubmitUnityMatchResultCommand.FromPayload(payload);
        cmd.MatchId.Should().NotBeNullOrEmpty();
        cmd.Result.Should().Be("lose");
        cmd.Scene.Should().Be("unknown");
        cmd.TotalPlayers.Should().Be(0);
        cmd.UserId.Should().BeNull();
        cmd.StartUtc.Should().BeOnOrAfter(before.AddMinutes(-6));
        cmd.EndUtc.Should().BeOnOrAfter(before.AddMinutes(-1));
        cmd.RawJson.Should().Contain("[");
    }

    [Fact(Skip="Disabled per request")]
    public void FromPayload_UndefinedElement_RawJsonFallback_AndDefaults()
    {
        var cmd = SubmitUnityMatchResultCommand.FromPayload(default);
        cmd.RawJson.Should().Be("{}");
        cmd.Result.Should().Be("lose");
        cmd.Scene.Should().Be("unknown");
        cmd.TotalPlayers.Should().Be(0);
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_DuplicateKey_MergesExisting_FieldsUpdated()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = sessionId.ToString(),
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 3,
            UserId = userId,
            RawJson = JsonSerializer.Serialize(new { playerSummaries = Array.Empty<object>() })
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<MatchResult>(new Exception("duplicate key value")));
        var existing = new MatchResult { Id = Guid.NewGuid(), MatchId = cmd.MatchId, Result = "lose", Scene = "old", StartUtc = default, EndUtc = default, TotalPlayers = 1, UserId = null, MatchDataJson = "{}" };
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<MatchResult?>(null),
            Task.FromResult<MatchResult?>(existing)
        );
        MatchResult? updated = null;
        mrRepo.UpdateAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { updated = ci.Arg<MatchResult>(); return Task.FromResult(updated!); });

        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" }));
        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.Result.Should().Be("win");
        updated.Scene.Should().Be("main");
        updated.TotalPlayers.Should().Be(3);
        updated.StartUtc.Kind.Should().Be(DateTimeKind.Utc);
        updated.EndUtc.Kind.Should().Be(DateTimeKind.Utc);
        updated.UserId.Should().Be(userId);
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_ResolvePhase_UserNearestSession_Selected()
    {
        var userId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = "not-a-guid",
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-6),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = userId,
            RawJson = JsonSerializer.Serialize(new { })
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });

        var s1 = new GameSession { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Status = "created", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-20) };
        var s2 = new GameSession { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Status = "created", CompletedAt = DateTimeOffset.UtcNow.AddMinutes(-1), CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30) };
        var recent = new System.Collections.Generic.List<GameSession> { s1, s2 };
        gsRepo.GetRecentSessionsByUserAsync(userId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(recent));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        // Resolve phase chose s2 (closest pivot using CompletedAt)
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Id == s2.Id && s.Status == "completed"), Arg.Any<CancellationToken>());
    }
    [Fact(Skip="Disabled per request")]
    public async Task Handle_DuplicateKey_MergesExisting_UserIdPatched()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cmd = BuildCommand(sessionId, userId);

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<MatchResult>(new Exception("duplicate key value")));
        var existing = new MatchResult { Id = Guid.NewGuid(), MatchId = cmd.MatchId, Result = "lose", Scene = "old", StartUtc = DateTime.UtcNow.AddMinutes(-30), EndUtc = DateTime.UtcNow.AddMinutes(-20), UserId = null };
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<MatchResult?>(null),
            Task.FromResult<MatchResult?>(existing)
        );
        mrRepo.UpdateAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => {
            var m = ci.Arg<MatchResult>();
            if (m.Id == Guid.Empty) m.Id = Guid.NewGuid();
            return Task.FromResult(m);
        });

        gsRepo.GetRecentSessionsByUserAsync(userId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new System.Collections.Generic.List<GameSession>()));
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(null));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await mrRepo.Received(1).UpdateAsync(Arg.Is<MatchResult>(m => m.UserId == userId), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_GuidLinking_WhenResolveReturnsNull_ThenFoundByGuid()
    {
        var sessionId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = sessionId.ToString(),
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-5),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = null,
            RawJson = JsonSerializer.Serialize(new { })
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<MatchResult>(new Exception("duplicate key value")));

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(null), Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        await gsRepo.Received(2).GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_LookupByMatchId_Throws_Warns_And_AddsNew()
    {
        var sessionId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = sessionId.ToString(),
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = null,
            RawJson = JsonSerializer.Serialize(new { })
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(
            Task.FromException<MatchResult?>(new Exception("db error")),
            Task.FromResult<MatchResult?>(null)
        );
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<MatchResult>(new Exception("duplicate key value")));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        await mrRepo.Received(1).AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_AddAsync_NonDuplicate_Throws_Rethrows()
    {
        var sessionId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand { MatchId = sessionId.ToString(), Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-2), EndUtc = DateTime.UtcNow, TotalPlayers = 1, RawJson = "{}" };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<MatchResult>(new Exception("boom")));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await Assert.ThrowsAsync<Exception>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_NoSessionFound_LogsWarning()
    {
        var cmd = new SubmitUnityMatchResultCommand { MatchId = "not-a-guid", Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-1), EndUtc = DateTime.UtcNow, TotalPlayers = 0, UserId = null, RawJson = "{not json" };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });
        gsRepo.GetRecentSessionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new System.Collections.Generic.List<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_SessionIdForSummaries_FromMatchIdGuid()
    {
        var sessionId = Guid.NewGuid();
        var raw = JsonSerializer.Serialize(new { playerSummaries = new[] { new { playerId = 7L, userId = Guid.NewGuid().ToString(), totalQuestions = 2, correctAnswers = 1 } } });
        var cmd = new SubmitUnityMatchResultCommand { MatchId = sessionId.ToString(), Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-3), EndUtc = DateTime.UtcNow, TotalPlayers = 1, UserId = null, RawJson = raw };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await sumRepo.Received(1).AddRangeAsync(Arg.Is<System.Collections.Generic.IReadOnlyList<MatchPlayerSummary>>(l => l.Count == 1 && l[0].SessionId == sessionId), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_PackMerge_QuestionsMissingNonArray_Injects()
    {
        var sessionId = Guid.NewGuid();
        var pack = JsonSerializer.Serialize(new { questions = new[] { new { id = 1 }, new { id = 2 } } });
        var raw = JsonSerializer.Serialize(new { questions = "oops" });
        var cmd = new SubmitUnityMatchResultCommand { MatchId = sessionId.ToString(), Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-5), EndUtc = DateTime.UtcNow, TotalPlayers = 1, UserId = null, RawJson = raw };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        MatchResult? added = null;
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { added = ci.Arg<MatchResult>(); added!.Id = Guid.NewGuid(); return Task.FromResult(added); });

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created", QuestionPackJson = pack };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        var json1 = added!.MatchDataJson ?? "{}";
        var node = System.Text.Json.Nodes.JsonNode.Parse(json1) as System.Text.Json.Nodes.JsonObject;
        var questions = node!["questions"] as System.Text.Json.Nodes.JsonArray;
        questions!.Count.Should().Be(2);
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_MergeMatchData_ExistingNoQuestions_InjectIncoming()
    {
        var sessionId = Guid.NewGuid();
        var incomingRaw = JsonSerializer.Serialize(new { questions = new[] { new { id = 1 } } });
        var cmd = new SubmitUnityMatchResultCommand { MatchId = sessionId.ToString(), Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-5), EndUtc = DateTime.UtcNow, TotalPlayers = 1, RawJson = incomingRaw };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        var existing = new MatchResult { Id = Guid.NewGuid(), MatchId = cmd.MatchId, MatchDataJson = "{}", StartUtc = DateTime.UtcNow.AddMinutes(-6), EndUtc = DateTime.UtcNow.AddMinutes(-3), Result = "lose", Scene = "old", TotalPlayers = 2 };
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(existing));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });
        MatchResult? updatedArg = null;
        mrRepo.UpdateAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { updatedArg = ci.Arg<MatchResult>(); return Task.FromResult(updatedArg); });

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await mrRepo.Received(1).UpdateAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_PlayerSummaries_ParseFails_SkipsAdd()
    {
        var cmd = new SubmitUnityMatchResultCommand { MatchId = Guid.NewGuid().ToString(), Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-5), EndUtc = DateTime.UtcNow, TotalPlayers = 1, RawJson = "{not json" };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await sumRepo.Received(1).DeleteByMatchResultIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await sumRepo.DidNotReceive().AddRangeAsync(Arg.Any<System.Collections.Generic.IReadOnlyList<MatchPlayerSummary>>(), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_PlayerSummaries_NonNumeric_ParseNulls()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var raw = JsonSerializer.Serialize(new
        {
            per_player = new[] { new { user_id = userId.ToString(), client_id = "abc", summary = new { topics = new[]{ new { total = 1, correct = 1 } } } } },
            playerSummaries = new[] { new { playerId = 1, userId = userId.ToString(), totalQuestions = 2, correctAnswers = 2, averageTime = "abc" } }
        });

        var cmd = new SubmitUnityMatchResultCommand { MatchId = sessionId.ToString(), Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-5), EndUtc = DateTime.UtcNow, TotalPlayers = 2, UserId = userId, RawJson = raw };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" }));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await sumRepo.Received(1).AddRangeAsync(Arg.Is<System.Collections.Generic.IReadOnlyList<MatchPlayerSummary>>(l =>
            System.Linq.Enumerable.Any(l, x => x.ClientId == null) &&
            System.Linq.Enumerable.Any(l, x => !x.AverageTime.HasValue)
        ), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_PlayerSummaries_DeleteThrows_CatchesAndContinues()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var raw = JsonSerializer.Serialize(new { playerSummaries = new[] { new { playerId = 2L, userId = userId.ToString(), totalQuestions = 1, correctAnswers = 1 } } });
        var cmd = new SubmitUnityMatchResultCommand { MatchId = sessionId.ToString(), Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-5), EndUtc = DateTime.UtcNow, TotalPlayers = 1, UserId = userId, RawJson = raw };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" }));
        sumRepo.DeleteByMatchResultIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException(new Exception("del fail")));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        await sumRepo.DidNotReceive().AddRangeAsync(Arg.Any<System.Collections.Generic.IReadOnlyList<MatchPlayerSummary>>(), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_Parsers_StringValues_ForLongAndDouble()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var raw = JsonSerializer.Serialize(new
        {
            per_player = new[]
            {
                new { user_id = userId.ToString(), client_id = "123", summary = new { topics = new[]{ new { total = 2, correct = 2 } } } }
            },
            playerSummaries = new[]
            {
                new { playerId = 999, userId = userId.ToString(), totalQuestions = 4, correctAnswers = 3, averageTime = "1.75" }
            }
        });

        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = sessionId.ToString(),
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-5),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 2,
            UserId = userId,
            RawJson = raw
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await sumRepo.Received(1).AddRangeAsync(
            Arg.Is<System.Collections.Generic.IReadOnlyList<MatchPlayerSummary>>(l =>
                l.Count == 2 &&
                System.Linq.Enumerable.Any(l, x => x.ClientId == 123) &&
                System.Linq.Enumerable.Any(l, x => x.AverageTime.HasValue && Math.Abs(x.AverageTime.Value - 1.75) < 0.0001)
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_DuplicateKey_FilterVariant_MergesExisting()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cmd = BuildCommand(sessionId, userId);

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<MatchResult>(new Exception("match_results_match_id_key violation")));
        var existing = new MatchResult { Id = Guid.NewGuid(), MatchId = cmd.MatchId, Result = "lose", Scene = "old", StartUtc = DateTime.UtcNow.AddMinutes(-30), EndUtc = DateTime.UtcNow.AddMinutes(-20) };
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(existing));
        mrRepo.UpdateAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<MatchResult>()));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        await mrRepo.Received(1).UpdateAsync(Arg.Is<MatchResult>(m => m.Result == "win" && m.Scene == "main"), Arg.Any<CancellationToken>());
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Status == "completed" && s.MatchResultId.HasValue), Arg.Any<CancellationToken>());
    }
    [Fact(Skip="Disabled per request")]
    public async Task Handle_PayloadSessionId_Used_WhenNoLinkage()
    {
        var sessionId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = "not-a-guid",
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-2),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = null,
            RawJson = JsonSerializer.Serialize(new { sessionId = sessionId.ToString(), playerSummaries = new[] { new { playerId = 1L, userId = Guid.NewGuid().ToString(), totalQuestions = 1, correctAnswers = 1 } } })
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => {
            var m = ci.Arg<MatchResult>();
            m.Id = Guid.NewGuid();
            return Task.FromResult(m);
        });

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        res.SessionId.Should().Be(sessionId);
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Status == "completed" && s.MatchResultId.HasValue), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_MergeQuestionPack_InjectsQuestions_WhenMissing()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var pack = JsonSerializer.Serialize(new { questions = new[] { new { id = 1 }, new { id = 2 }, new { id = 3 } }, other = "x" });
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = sessionId.ToString(),
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = userId,
            RawJson = "{}"
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        MatchResult? added = null;
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => {
            added = ci.Arg<MatchResult>();
            added!.Id = Guid.NewGuid();
            return Task.FromResult(added);
        });

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created", QuestionPackJson = pack };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        added.Should().NotBeNull();
        var json3 = added!.MatchDataJson ?? "{}";
        var node = System.Text.Json.Nodes.JsonNode.Parse(json3) as System.Text.Json.Nodes.JsonObject;
        var questions = node!["questions"] as System.Text.Json.Nodes.JsonArray;
        questions!.Count.Should().Be(3);
        node["questionPack"].Should().NotBeNull();
        node["sessionId"]!.ToString().Should().Be(sessionId.ToString());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_MergeQuestionPack_DoesNotOverride_WhenQuestionsPresent()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var pack = JsonSerializer.Serialize(new { questions = new[] { new { id = 9 } } });
        var raw = JsonSerializer.Serialize(new { questions = new[] { new { id = 1 }, new { id = 2 } } });
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = sessionId.ToString(),
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = userId,
            RawJson = raw
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        MatchResult? added = null;
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => {
            added = ci.Arg<MatchResult>();
            added!.Id = Guid.NewGuid();
            return Task.FromResult(added);
        });

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created", QuestionPackJson = pack };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        var json4 = added!.MatchDataJson ?? "{}";
        var node = System.Text.Json.Nodes.JsonNode.Parse(json4) as System.Text.Json.Nodes.JsonObject;
        var questions = node!["questions"] as System.Text.Json.Nodes.JsonArray;
        questions!.Count.Should().Be(2);
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_MergeMatchData_MergesFields_Questions_Summaries_ClientIds()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingJson = JsonSerializer.Serialize(new
        {
            playerSummaries = new[] { new { playerId = 1L, userId = userId.ToString(), totalQuestions = 1, correctAnswers = 0 } },
            playerClientIds = new[] { 1L, 2L },
            questions = new[] { new { id = 1 } },
            totalPlayers = 2,
            startUtc = DateTime.UtcNow.AddMinutes(-5).ToString("o"),
            endUtc = DateTime.UtcNow.AddMinutes(-2).ToString("o")
        });

        var incomingRaw = JsonSerializer.Serialize(new
        {
            playerSummaries = new[] { new { playerId = 1L, userId = userId.ToString(), totalQuestions = 5, correctAnswers = 4 }, new { playerId = 2L, userId = userId.ToString(), totalQuestions = 3, correctAnswers = 2 } },
            playerClientIds = new[] { 2L, 3L },
            questions = new[] { new { id = 1 }, new { id = 2 }, new { id = 3 } },
            questionPack = new { meta = "x" },
            relayRegion = "us",
            joinCode = "CODE",
            hostClientId = 777,
            userId = userId.ToString(),
            totalPlayers = "4",
            startUtc = DateTime.UtcNow.AddMinutes(-10).ToString("o"),
            endUtc = DateTime.UtcNow.AddMinutes(1).ToString("o")
        });

        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = sessionId.ToString(),
            Result = "win",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = userId,
            RawJson = incomingRaw
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        var existing = new MatchResult { Id = Guid.NewGuid(), MatchId = cmd.MatchId, MatchDataJson = existingJson, StartUtc = DateTime.UtcNow.AddMinutes(-6), EndUtc = DateTime.UtcNow.AddMinutes(-3), Result = "lose", Scene = "old", TotalPlayers = 2, UserId = userId };
        MatchResult? updated = null;
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(existing));
        mrRepo.UpdateAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => {
            updated = ci.Arg<MatchResult>();
            return Task.FromResult(updated!);
        });

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created", QuestionPackJson = null };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        updated.Should().NotBeNull();
        var json5 = updated!.MatchDataJson ?? "{}";
        var node = System.Text.Json.Nodes.JsonNode.Parse(json5) as System.Text.Json.Nodes.JsonObject;
        var summaries = node!["playerSummaries"] as System.Text.Json.Nodes.JsonArray;
        summaries!.Count.Should().Be(2);
        var ps1 = summaries[0] as System.Text.Json.Nodes.JsonObject;
        var ps2 = summaries[1] as System.Text.Json.Nodes.JsonObject;
        new[] { ps1!["playerId"]!.ToString(), ps2!["playerId"]!.ToString() }.Should().Contain(new[] { "1", "2" });
        var clientIds = node["playerClientIds"] as System.Text.Json.Nodes.JsonArray;
        clientIds!.Select(x => x!.ToString()).Should().BeEquivalentTo(new[] { "1", "2", "3" });
        var questions = node["questions"] as System.Text.Json.Nodes.JsonArray;
        questions!.Count.Should().Be(3);
        node["questionPack"].Should().NotBeNull();
        node["relayRegion"]!.ToString().Should().Be("us");
        node["joinCode"]!.ToString().Should().Be("CODE");
        node["hostClientId"]!.ToString().Should().Be("777");
        node["userId"]!.ToString().Should().Be(userId.ToString());
        node["totalPlayers"]!.ToString().Should().Be("4");
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_PlayerSummaries_DefaultAdded_WhenNonePresent()
    {
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = "not-a-guid",
            Result = "lose",
            Scene = "unknown",
            StartUtc = DateTime.UtcNow.AddMinutes(-3),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 0,
            UserId = null,
            RawJson = "{}"
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await sumRepo.Received(1).AddRangeAsync(Arg.Is<System.Collections.Generic.IReadOnlyList<MatchPlayerSummary>>(l => l.Count == 1 && l[0].TotalQuestions == 0 && l[0].CorrectAnswers == 0), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_DuplicateKey_MergesExisting_ThenLinks()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cmd = BuildCommand(sessionId, userId);

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<MatchResult>(new Exception("duplicate key value")));
        var existing = new MatchResult { Id = Guid.NewGuid(), MatchId = cmd.MatchId, Result = "lose", Scene = "old", StartUtc = DateTime.UtcNow.AddMinutes(-30), EndUtc = DateTime.UtcNow.AddMinutes(-20) };
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(existing));
        mrRepo.UpdateAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<MatchResult>()));

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        await mrRepo.Received(1).UpdateAsync(Arg.Is<MatchResult>(m => m.Result == "win" && m.Scene == "main"), Arg.Any<CancellationToken>());
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Status == "completed" && s.MatchResultId.HasValue), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_DuplicateKey_NoExisting_Fallback_ThenNearestUnlinked()
    {
        var userId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = "not-a-guid",
            Result = "win",
            Scene = "arena",
            StartUtc = DateTime.UtcNow.AddMinutes(-2),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = null,
            RawJson = JsonSerializer.Serialize(new { playerSummaries = Array.Empty<object>() })
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<MatchResult>(new Exception("duplicate key value")));
        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));

        var nearest = new GameSession { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Status = "created" , CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)};
        gsRepo.GetRecentSessionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new System.Collections.Generic.List<GameSession> { nearest }));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Status == "completed" && s.MatchResultId.HasValue), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_UserFallback_RecentUncompletedSession_Linked()
    {
        var userId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = "not-a-guid",
            Result = "win",
            Scene = "arena",
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = userId,
            RawJson = JsonSerializer.Serialize(new { })
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => {
            var m = ci.Arg<MatchResult>();
            m.Id = Guid.NewGuid();
            return Task.FromResult(m);
        });

        var recent = new System.Collections.Generic.List<GameSession>
        {
            new GameSession { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Status = "created", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30) },
            new GameSession { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Status = "created", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5) }
        };
        gsRepo.GetRecentSessionsByUserAsync(userId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new System.Collections.Generic.List<GameSession>()), Task.FromResult(recent));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Status == "completed" && s.MatchResultId.HasValue), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Handle_PlayerSummaries_TotalsZero_UseTopicBreakdownSum()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var raw = JsonSerializer.Serialize(new
        {
            sessionId = sessionId.ToString(),
            playerSummaries = new[]
            {
                new { playerId = 111L, userId = userId.ToString(), totalQuestions = 0, correctAnswers = 0, topicBreakdown = new[]{ new { total = 5, correct = 3 }, new { total = 2, correct = 2 } } }
            }
        });

        var cmd = new SubmitUnityMatchResultCommand { MatchId = sessionId.ToString(), Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-5), EndUtc = DateTime.UtcNow, TotalPlayers = 1, UserId = userId, RawJson = raw };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" }));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await sumRepo.Received(1).AddRangeAsync(Arg.Is<System.Collections.Generic.IReadOnlyList<MatchPlayerSummary>>(l => l.Count == 1 && l[0].TotalQuestions == 7 && l[0].CorrectAnswers == 5), Arg.Any<CancellationToken>());
        await sumRepo.Received(1).DeleteByMatchResultIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PerPlayer_Summaries_AreExtracted()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var raw = JsonSerializer.Serialize(new
        {
            per_player = new[]
            {
                new { user_id = userId.ToString(), client_id = 5L, summary = new { topics = new[]{ new { total = 2, correct = 1 } } } },
                new { user_id = userId.ToString(), client_id = 6L, summary = new { topics = new[]{ new { total = 3, correct = 2 } } } }
            }
        });

        var cmd = new SubmitUnityMatchResultCommand { MatchId = sessionId.ToString(), Result = "win", Scene = "main", StartUtc = DateTime.UtcNow.AddMinutes(-5), EndUtc = DateTime.UtcNow, TotalPlayers = 2, UserId = userId, RawJson = raw };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" }));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await sumRepo.Received(1).AddRangeAsync(Arg.Is<System.Collections.Generic.IReadOnlyList<MatchPlayerSummary>>(l => l.Count == 2 && l[0].TotalQuestions == 2 && l[1].TotalQuestions == 3), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DefaultResultAndScene_WhenMissing()
    {
        var sessionId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand { MatchId = sessionId.ToString(), Result = null, Scene = null, StartUtc = DateTime.UtcNow.AddMinutes(-3), EndUtc = DateTime.UtcNow, TotalPlayers = 1, RawJson = "{}" };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => { var m = ci.Arg<MatchResult>(); m.Id = Guid.NewGuid(); return Task.FromResult(m); });
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" });

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await mrRepo.Received(1).AddAsync(Arg.Is<MatchResult>(m => m.Result == "lose" && m.Scene == "unknown"), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_MatchIdGuid_LinksSession_AndSavesSummaries()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cmd = BuildCommand(sessionId, userId);

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var m = ci.Arg<MatchResult>();
            m.Id = Guid.NewGuid();
            return Task.FromResult(m);
        });

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created", UserId = null };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        res.MatchId.Should().Be(cmd.MatchId);
        res.SessionId.Should().Be(sessionId);
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Status == "completed" && s.MatchResultId.HasValue), Arg.Any<CancellationToken>());
        await sumRepo.Received(1).AddRangeAsync(Arg.Is<System.Collections.Generic.IReadOnlyList<MatchPlayerSummary>>(l => l.Count == 1 && l[0].SessionId == sessionId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ResolveByJoinCode_WhenMatchIdNotGuid_LinksSession()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cmd = new SubmitUnityMatchResultCommand
        {
            MatchId = "abc",
            JoinCode = "JOIN123",
            Result = "lose",
            Scene = "main",
            StartUtc = DateTime.UtcNow.AddMinutes(-5),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 2,
            UserId = userId,
            RawJson = JsonSerializer.Serialize(new { playerSummaries = Array.Empty<object>() })
        };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MatchResult?>(null));
        mrRepo.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var m = ci.Arg<MatchResult>();
            m.Id = Guid.NewGuid();
            return Task.FromResult(m);
        });

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, RelayJoinCode = cmd.JoinCode, Status = "created" };
        gsRepo.GetByJoinCodeAsync(cmd.JoinCode!, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GameSession?>(session));
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<GameSession>()));

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        res.SessionId.Should().Be(sessionId);
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Status == "completed" && s.MatchResultId.HasValue), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingMatch_UpdatesInsteadOfInsert()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cmd = BuildCommand(sessionId, userId);

        var existing = new MatchResult { Id = Guid.NewGuid(), MatchId = cmd.MatchId, StartUtc = DateTime.UtcNow.AddMinutes(-20), EndUtc = DateTime.UtcNow.AddMinutes(-10), Result = "lose", Scene = "old" };

        var mrRepo = Substitute.For<IMatchResultRepository>();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var sumRepo = Substitute.For<IMatchPlayerSummaryRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmitUnityMatchResultHandler>>();

        mrRepo.GetByMatchIdAsync(cmd.MatchId, Arg.Any<CancellationToken>()).Returns(existing);
        mrRepo.UpdateAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<MatchResult>());

        var session = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created" };
        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(session);

        var sut = CreateSut(mrRepo, gsRepo, sumRepo, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Success.Should().BeTrue();
        res.MatchId.Should().Be(cmd.MatchId);
        await mrRepo.Received(1).UpdateAsync(Arg.Is<MatchResult>(m => m.Result == "win" && m.Scene == "main"), Arg.Any<CancellationToken>());
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Status == "completed" && s.MatchResultId.HasValue), Arg.Any<CancellationToken>());
    }
}
