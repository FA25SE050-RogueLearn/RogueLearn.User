using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.JoinPublicParty;

public record JoinPublicPartyCommand(Guid PartyId, Guid AuthUserId) : IRequest<Unit>;