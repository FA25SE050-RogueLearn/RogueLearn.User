using MediatR;

namespace RogueLearn.User.Application.Features.UnityMatches.Queries.GetUnityMatches;

public record GetUnityMatchesQuery(int Limit = 10, string? UserId = null) : IRequest<string>;

