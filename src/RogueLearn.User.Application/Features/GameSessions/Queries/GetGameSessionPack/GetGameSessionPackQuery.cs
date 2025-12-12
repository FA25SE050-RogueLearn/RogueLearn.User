using MediatR;

namespace RogueLearn.User.Application.Features.GameSessions.Queries.GetGameSessionPack;

public sealed record GetGameSessionPackQuery(Guid SessionId) : IRequest<string?>;

