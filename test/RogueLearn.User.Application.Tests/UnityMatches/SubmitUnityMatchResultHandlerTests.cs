using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.UnityMatches.Commands.SubmitUnityMatchResult;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.UnityMatches;

public class SubmitUnityMatchResultHandlerTests
{
    private readonly IMatchResultRepository _matchResultRepository = Substitute.For<IMatchResultRepository>();
    private readonly IGameSessionRepository _gameSessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly IMatchPlayerSummaryRepository _summaryRepository = Substitute.For<IMatchPlayerSummaryRepository>();
    private readonly ISkillRepository _skillRepository = Substitute.For<ISkillRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<SubmitUnityMatchResultHandler> _logger = Substitute.For<ILogger<SubmitUnityMatchResultHandler>>();

    [Fact(Skip="Disabled per request")]
    public async Task Adds_new_match_and_links_session_by_match_id()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var matchResultId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _matchResultRepository.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var incoming = ci.Arg<MatchResult>();
                incoming.Id = matchResultId;
                return incoming;
            });

        _gameSessionRepository.GetBySessionIdAsync(matchId, Arg.Any<CancellationToken>())
            .Returns(new GameSession { SessionId = matchId, Status = "created" });

        _summaryRepository.DeleteByMatchResultIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _skillRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Skill>());

        var handler = new SubmitUnityMatchResultHandler(
            _matchResultRepository,
            _gameSessionRepository,
            _summaryRepository,
            _skillRepository,
            _mediator,
            _logger);

        var rawJson = JsonSerializer.Serialize(new
        {
            per_player = new[]
            {
                new
                {
                    user_id = userId,
                    client_id = 1,
                    summary = new
                    {
                        topics = new[]
                        {
                            new { topic = "t1", total = 2, correct = 1 }
                        }
                    }
                }
            }
        });

        var command = new SubmitUnityMatchResultCommand
        {
            MatchId = matchId.ToString(),
            Result = "win",
            JoinCode = null,
            Scene = "arena",
            StartUtc = DateTime.UtcNow.AddMinutes(-5),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1,
            UserId = userId,
            RawJson = rawJson
        };

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.SessionId.Should().Be(matchId);
        await _matchResultRepository.Received(1).AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>());
        await _summaryRepository.Received(1).DeleteByMatchResultIdAsync(matchResultId, Arg.Any<CancellationToken>());
        await _summaryRepository.Received(1).AddRangeAsync(Arg.Is<IEnumerable<MatchPlayerSummary>>(s => s.Count() == 1), Arg.Any<CancellationToken>());
        await _gameSessionRepository.Received(1).UpdateAsync(Arg.Is<GameSession>(s => s.MatchResultId == matchResultId && s.Status == "completed"), Arg.Any<CancellationToken>());
    }

    [Fact(Skip="Disabled per request")]
    public async Task Updates_existing_match_on_duplicate_match_id()
    {
        // Arrange
        var matchId = "dup";
        var existing = new MatchResult { Id = Guid.NewGuid(), MatchId = matchId, MatchDataJson = "{}", Result = "lose" };

        _matchResultRepository.AddAsync(Arg.Any<MatchResult>(), Arg.Any<CancellationToken>())
            .Returns<Task<MatchResult>>(x => throw new Exception("duplicate key value"));

        _matchResultRepository.GetByMatchIdAsync(matchId, Arg.Any<CancellationToken>())
            .Returns(existing);

        _matchResultRepository.UpdateAsync(existing, Arg.Any<CancellationToken>())
            .Returns(existing);

        _summaryRepository.DeleteByMatchResultIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _skillRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Skill>());

        var handler = new SubmitUnityMatchResultHandler(
            _matchResultRepository,
            _gameSessionRepository,
            _summaryRepository,
            _skillRepository,
            _mediator,
            _logger);

        var command = new SubmitUnityMatchResultCommand
        {
            MatchId = matchId,
            Result = "win",
            RawJson = "{}",
            StartUtc = DateTime.UtcNow.AddMinutes(-5),
            EndUtc = DateTime.UtcNow,
            TotalPlayers = 1
        };

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.MatchId.Should().Be(matchId);
        await _matchResultRepository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }
}
