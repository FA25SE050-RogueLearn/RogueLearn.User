using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPartyResources;

public record GetPartyResourcesQuery(Guid PartyId) : IRequest<IReadOnlyList<PartyStashItemDto>>;