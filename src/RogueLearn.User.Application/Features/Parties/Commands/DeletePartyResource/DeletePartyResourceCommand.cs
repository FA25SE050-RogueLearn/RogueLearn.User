using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.DeletePartyResource;

public record DeletePartyResourceCommand(
    Guid PartyId,
    Guid StashItemId,
    Guid ActorAuthUserId
) : IRequest<Unit>;