using MediatR;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Exceptions;

namespace RogueLearn.User.Application.Features.Parties.Commands.DeleteParty;

public class DeletePartyCommandHandler : IRequestHandler<DeletePartyCommand, Unit>
{
    private readonly IPartyRepository _partyRepository;

    public DeletePartyCommandHandler(IPartyRepository partyRepository)
    {
        _partyRepository = partyRepository;
    }

    public async Task<Unit> Handle(DeletePartyCommand request, CancellationToken cancellationToken)
    {
        var party = await _partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new NotFoundException("Party", request.PartyId.ToString());

        await _partyRepository.DeleteAsync(party.Id, cancellationToken);
        return Unit.Value;
    }
}