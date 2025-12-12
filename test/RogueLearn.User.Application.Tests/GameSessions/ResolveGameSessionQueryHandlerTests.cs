using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.GameSessions.Queries.ResolveGameSession;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.GameSessions;

public class ResolveGameSessionQueryHandlerTests
{
    private readonly IGameSessionRepository _repo = Substitute.For<IGameSessionRepository>();

    [Fact]
    public async Task Returns_session_by_join_code_even_with_whitespace_and_case()
    {
        // Arrange
        var session = new GameSession { SessionId = Guid.NewGuid(), RelayJoinCode = "ABC123" };
        _repo.GetByJoinCodeAsync("ABC123", Arg.Any<CancellationToken>())
            .Returns(session);
        var handler = new ResolveGameSessionQueryHandler(_repo);

        // Act
        var result = await handler.Handle(new ResolveGameSessionQuery("  abc123  ", null), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(session.SessionId);
        await _repo.Received().GetByJoinCodeAsync("ABC123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falls_back_to_match_id_when_join_code_not_found()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _repo.GetByJoinCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((GameSession?)null);
        _repo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new GameSession { SessionId = sessionId });
        var handler = new ResolveGameSessionQueryHandler(_repo);

        // Act
        var result = await handler.Handle(new ResolveGameSessionQuery(null, sessionId.ToString()), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(sessionId);
        await _repo.Received().GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_null_when_session_not_found_by_code_or_id()
    {
        // Arrange
        _repo.GetByJoinCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((GameSession?)null);
        _repo.GetBySessionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((GameSession?)null);
        var handler = new ResolveGameSessionQueryHandler(_repo);

        // Act
        var result = await handler.Handle(new ResolveGameSessionQuery("MISSING", "00000000-0000-0000-0000-000000000000"), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
