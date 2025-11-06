using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.DeclineInvitation;

public record DeclinePartyInvitationCommand(Guid PartyId, Guid InvitationId, Guid AuthUserId) : IRequest<Unit>;