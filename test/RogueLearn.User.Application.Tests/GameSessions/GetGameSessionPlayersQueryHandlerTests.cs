using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.GameSessions.Queries.GetGameSessionPlayers;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.GameSessions;

public class GetGameSessionPlayersQueryHandlerTests
{
    private readonly IMatchPlayerSummaryRepository _summaryRepo = Substitute.For<IMatchPlayerSummaryRepository>();
    private readonly IGameSessionRepository _sessionRepo = Substitute.For<IGameSessionRepository>();

    [Fact]
    public async Task Returns_summaries_for_session()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var summaries = new List<MatchPlayerSummary>
        {
            new() { Id = Guid.NewGuid(), SessionId = sessionId, UserId = Guid.NewGuid(), ClientId = 1, TotalQuestions = 5, CorrectAnswers = 4, AverageTime = 1.2, TopicBreakdownJson = "{\"topics\":[{\"topic\":\"t1\"}]}", CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), SessionId = sessionId, UserId = Guid.NewGuid(), ClientId = 2, TotalQuestions = 3, CorrectAnswers = 2, AverageTime = 2.5, TopicBreakdownJson = null, CreatedAt = DateTimeOffset.UtcNow }
        };
        _summaryRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(summaries);

        var handler = new GetGameSessionPlayersQueryHandler(_summaryRepo, _sessionRepo);

        // Act
        var result = await handler.Handle(new GetGameSessionPlayersQuery(sessionId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First().TopicBreakdown.Should().NotBeNull();
        result.First().AverageTime.Should().Be(1.2);
        result.Last().AverageTime.Should().Be(2.5);
    }

    [Fact]
    public async Task Falls_back_to_match_result_when_session_has_match_result()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var matchResultId = Guid.NewGuid();
        _summaryRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<MatchPlayerSummary>());

        _sessionRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new GameSession { SessionId = sessionId, MatchResultId = matchResultId });

        var fallbackSummaries = new List<MatchPlayerSummary>
        {
            new() { Id = Guid.NewGuid(), MatchResultId = matchResultId, SessionId = sessionId, TotalQuestions = 4, CorrectAnswers = 3, AverageTime = null, CreatedAt = DateTimeOffset.UtcNow }
        };
        _summaryRepo.GetByMatchResultIdAsync(matchResultId, Arg.Any<CancellationToken>())
            .Returns(fallbackSummaries);

        var handler = new GetGameSessionPlayersQueryHandler(_summaryRepo, _sessionRepo);

        // Act
        var result = await handler.Handle(new GetGameSessionPlayersQuery(sessionId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Single().AverageTime.Should().Be(0d); // null average_time becomes 0
    }
}
