using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPartyResourceById;

public record GetPartyResourceByIdQuery(Guid PartyId, Guid StashItemId) : IRequest<PartyStashItemDto?>;