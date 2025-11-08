using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.DeletePartyResource;

public class DeletePartyResourceCommandHandler : IRequestHandler<DeletePartyResourceCommand, Unit>
{
    private readonly IPartyStashItemRepository _stashRepository;

    public DeletePartyResourceCommandHandler(IPartyStashItemRepository stashRepository)
    {
        _stashRepository = stashRepository;
    }

    public async Task<Unit> Handle(DeletePartyResourceCommand request, CancellationToken cancellationToken)
    {
        var item = await _stashRepository.GetByIdAsync(request.StashItemId, cancellationToken)
            ?? throw new NotFoundException("PartyStashItem", request.StashItemId.ToString());

        if (item.PartyId != request.PartyId)
        {
            throw new ForbiddenException("Stash item does not belong to the specified party");
        }

        await _stashRepository.DeleteAsync(item.Id, cancellationToken);
        return Unit.Value;
    }
}