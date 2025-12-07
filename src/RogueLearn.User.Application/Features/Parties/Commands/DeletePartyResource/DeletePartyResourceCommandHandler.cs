using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.DeletePartyResource;

public class DeletePartyResourceCommandHandler : IRequestHandler<DeletePartyResourceCommand, Unit>
{
    private readonly IPartyStashItemRepository _stashRepository;
    private readonly IPartyMemberRepository _memberRepository;

    public DeletePartyResourceCommandHandler(IPartyStashItemRepository stashRepository, IPartyMemberRepository memberRepository)
    {
        _stashRepository = stashRepository;
        _memberRepository = memberRepository;
    }

    public async Task<Unit> Handle(DeletePartyResourceCommand request, CancellationToken cancellationToken)
    {
        var item = await _stashRepository.GetByIdAsync(request.StashItemId, cancellationToken)
            ?? throw new NotFoundException("PartyStashItem", request.StashItemId.ToString());

        if (item.PartyId != request.PartyId)
        {
            throw new ForbiddenException("Stash item does not belong to the specified party");
        }

        var member = await _memberRepository.GetMemberAsync(request.PartyId, request.ActorAuthUserId, cancellationToken);
        if (member is null || member.Status != MemberStatus.Active)
        {
            throw new ForbiddenException("Actor is not an active party member");
        }

        if (member.Role != PartyRole.Leader && item.SharedByUserId != request.ActorAuthUserId)
        {
            throw new ForbiddenException("Only the sharer or party leader may delete this resource");
        }

        await _stashRepository.DeleteAsync(item.Id, cancellationToken);
        return Unit.Value;
    }
}
