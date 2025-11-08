using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Commands.UpdatePartyResource;

public record UpdatePartyResourceCommand(
    Guid PartyId,
    Guid StashItemId,
    Guid ActorAuthUserId,
    UpdatePartyResourceRequest Request
) : IRequest<Unit>;