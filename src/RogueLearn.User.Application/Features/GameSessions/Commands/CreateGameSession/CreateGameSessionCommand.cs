using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.GameSessions.Commands.CreateGameSession;

public sealed class CreateGameSessionCommand : IRequest<GameSession>
{
    public Guid SessionId { get; init; }
    public Guid? UserId { get; init; }
    public string? RelayJoinCode { get; init; }
    public string? PackSpecJson { get; init; }
    public string? PackId { get; init; }
    public string? Subject { get; init; }
    public string? Topic { get; init; }
    public string? Difficulty { get; init; }
    public string? QuestionPackJson { get; init; }
}
