using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Commands.AddPartyResource;

public record AddPartyResourceCommand(
    Guid PartyId,
    Guid SharedByUserId,
    string Title,
    IReadOnlyDictionary<string, object> Content,
    IReadOnlyList<string> Tags
) : IRequest<PartyStashItemDto>;