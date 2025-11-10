using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Commands.AddPartyResource;

public record AddPartyResourceCommand(
    Guid PartyId,
    Guid SharedByUserId,
    Guid OriginalNoteId,
    string Title,
    object Content,
    IReadOnlyList<string> Tags
) : IRequest<PartyStashItemDto>;