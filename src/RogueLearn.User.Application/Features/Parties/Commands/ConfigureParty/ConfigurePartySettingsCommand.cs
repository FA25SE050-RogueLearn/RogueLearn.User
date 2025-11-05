using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.ConfigureParty;

public record ConfigurePartySettingsCommand(Guid PartyId, string Name, string Description, string Privacy, int MaxMembers)
    : IRequest<Unit>;