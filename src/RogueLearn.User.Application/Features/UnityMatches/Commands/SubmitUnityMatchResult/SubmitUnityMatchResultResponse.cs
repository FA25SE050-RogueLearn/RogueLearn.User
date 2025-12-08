namespace RogueLearn.User.Application.Features.UnityMatches.Commands.SubmitUnityMatchResult;

public sealed class SubmitUnityMatchResultResponse
{
    public bool Success { get; init; }
    public string MatchId { get; init; } = string.Empty;
    public Guid? SessionId { get; init; }
}
