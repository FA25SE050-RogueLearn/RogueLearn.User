using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.ConfigureParty;

public class ConfigurePartySettingsCommandHandler : IRequestHandler<ConfigurePartySettingsCommand, Unit>
{
    private readonly IPartyRepository _partyRepository;

    public ConfigurePartySettingsCommandHandler(IPartyRepository partyRepository)
    {
        _partyRepository = partyRepository;
    }

    public async Task<Unit> Handle(ConfigurePartySettingsCommand request, CancellationToken cancellationToken)
    {
        var party = await _partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException("Party", request.PartyId.ToString());

        party.Name = request.Name;
        party.Description = request.Description;
        party.IsPublic = request.Privacy.Equals("public", StringComparison.OrdinalIgnoreCase);
        party.MaxMembers = request.MaxMembers;
        party.UpdatedAt = DateTimeOffset.UtcNow;

        await _partyRepository.UpdateAsync(party, cancellationToken);
        return Unit.Value;
    }
}