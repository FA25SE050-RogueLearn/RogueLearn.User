using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.RemoveMember;

public class RemovePartyMemberCommandHandler : IRequestHandler<RemovePartyMemberCommand, Unit>
{
    private readonly IPartyMemberRepository _memberRepository;
    private readonly IPartyNotificationService? _notificationService;

    public RemovePartyMemberCommandHandler(IPartyMemberRepository memberRepository, IPartyNotificationService notificationService)
    {
        _memberRepository = memberRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(RemovePartyMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("PartyMember", request.MemberId.ToString());

        if (member.PartyId != request.PartyId)
        {
            throw new Exceptions.BadRequestException("Member does not belong to target party.");
        }

        if (member.Role == PartyRole.Leader)
        {
            throw new Exceptions.BadRequestException("Cannot remove Leader. Transfer leadership first.");
        }

        await _memberRepository.DeleteAsync(member.Id, cancellationToken);
        if (_notificationService != null)
        {
            await _notificationService.SendMemberRemovedNotificationAsync(request.PartyId, member.AuthUserId, cancellationToken);
        }

        return Unit.Value;
    }
}