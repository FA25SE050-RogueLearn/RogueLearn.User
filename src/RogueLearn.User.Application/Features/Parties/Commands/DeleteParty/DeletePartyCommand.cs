using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.DeleteParty;

public record DeletePartyCommand(Guid PartyId) : IRequest<Unit>;