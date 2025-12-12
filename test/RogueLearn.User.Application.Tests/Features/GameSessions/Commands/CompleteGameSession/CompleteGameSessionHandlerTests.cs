using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.GameSessions.Commands.CompleteGameSession;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GameSessions.Commands.CompleteGameSession;

public class CompleteGameSessionHandlerTests
{
    [Fact]
    public async Task Handle_NotCompleted_UpdatesStatus_AndLinksMatch()
    {
        var sessionId = Guid.NewGuid();
        var gameSession = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "created", UserId = null };
        var match = new MatchResult { Id = Guid.NewGuid(), MatchId = sessionId.ToString(), UserId = Guid.NewGuid() };

        var gsRepo = Substitute.For<IGameSessionRepository>();
        var mrRepo = Substitute.For<IMatchResultRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CompleteGameSessionHandler>>();

        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(gameSession);
        mrRepo.GetByMatchIdAsync(sessionId.ToString(), Arg.Any<CancellationToken>()).Returns(match);
        gsRepo.UpdateAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GameSession>());

        var sut = new CompleteGameSessionHandler(gsRepo, mrRepo, logger);
        var res = await sut.Handle(new CompleteGameSessionCommand(sessionId), CancellationToken.None);

        res.MatchId.Should().Be(sessionId.ToString());
        res.AlreadyCompleted.Should().BeFalse();
        await gsRepo.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.Status == "completed" && s.MatchResultId == match.Id && s.UserId == match.UserId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyCompleted_DoesNotUpdate()
    {
        var sessionId = Guid.NewGuid();
        var gameSession = new GameSession { Id = Guid.NewGuid(), SessionId = sessionId, Status = "completed" };

        var gsRepo = Substitute.For<IGameSessionRepository>();
        var mrRepo = Substitute.For<IMatchResultRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CompleteGameSessionHandler>>();

        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(gameSession);

        var sut = new CompleteGameSessionHandler(gsRepo, mrRepo, logger);
        var res = await sut.Handle(new CompleteGameSessionCommand(sessionId), CancellationToken.None);

        res.MatchId.Should().Be(sessionId.ToString());
        res.AlreadyCompleted.Should().BeTrue();
        await gsRepo.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsNotCompleted_WithoutUpdate()
    {
        var sessionId = Guid.NewGuid();
        var gsRepo = Substitute.For<IGameSessionRepository>();
        var mrRepo = Substitute.For<IMatchResultRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CompleteGameSessionHandler>>();

        gsRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns((GameSession?)null);

        var sut = new CompleteGameSessionHandler(gsRepo, mrRepo, logger);
        var res = await sut.Handle(new CompleteGameSessionCommand(sessionId), CancellationToken.None);

        res.MatchId.Should().Be(sessionId.ToString());
        res.AlreadyCompleted.Should().BeFalse();
        await gsRepo.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!);
    }
}

