using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPartyById;

public record GetPartyByIdQuery(Guid PartyId) : IRequest<PartyDto?>;