using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.LeaveParty;

public record LeavePartyCommand(Guid PartyId, Guid AuthUserId) : IRequest<Unit>;