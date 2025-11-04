using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetMyParties;

public record GetMyPartiesQuery(Guid AuthUserId) : IRequest<IReadOnlyList<PartyDto>>;