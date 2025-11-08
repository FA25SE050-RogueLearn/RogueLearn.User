using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.UpdatePartyResource;

public class UpdatePartyResourceCommandHandler : IRequestHandler<UpdatePartyResourceCommand, Unit>
{
    private readonly IPartyStashItemRepository _stashRepository;

    public UpdatePartyResourceCommandHandler(IPartyStashItemRepository stashRepository)
    {
        _stashRepository = stashRepository;
    }

    public async Task<Unit> Handle(UpdatePartyResourceCommand request, CancellationToken cancellationToken)
    {
        var item = await _stashRepository.GetByIdAsync(request.StashItemId, cancellationToken)
            ?? throw new NotFoundException("PartyStashItem", request.StashItemId.ToString());

        if (item.PartyId != request.PartyId)
        {
            throw new ForbiddenException("Stash item does not belong to the specified party");
        }

        item.Title = request.Request.Title;
        item.Content = new Dictionary<string, object>(request.Request.Content);
        item.Tags = request.Request.Tags?.ToArray();
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _stashRepository.UpdateAsync(item, cancellationToken);
        return Unit.Value;
    }
}