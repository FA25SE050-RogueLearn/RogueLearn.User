namespace RogueLearn.User.Application.Features.GameSessions.Commands.CompleteGameSession;

public sealed class CompleteGameSessionResponse
{
    public string MatchId { get; init; } = string.Empty;
    public bool AlreadyCompleted { get; init; }
}
