using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.AcceptInvitation;

public record AcceptPartyInvitationCommand(Guid PartyId, Guid InvitationId, Guid AuthUserId) : IRequest<Unit>;