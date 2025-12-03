using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.TransferLeadership;

public class TransferPartyLeadershipCommandHandler : IRequestHandler<TransferPartyLeadershipCommand, Unit>
{
    private readonly IPartyMemberRepository _memberRepository;
    private readonly IPartyNotificationService? _notificationService;

    public TransferPartyLeadershipCommandHandler(IPartyMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
        _notificationService = null;
    }

    public TransferPartyLeadershipCommandHandler(IPartyMemberRepository memberRepository, IPartyNotificationService notificationService)
    {
        _memberRepository = memberRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(TransferPartyLeadershipCommand request, CancellationToken cancellationToken)
    {
        var members = await _memberRepository.GetMembersByPartyAsync(request.PartyId, cancellationToken);

        var newLeader = members.FirstOrDefault(m => m.AuthUserId == request.ToUserId && m.Status == MemberStatus.Active)
                        ?? throw new Application.Exceptions.NotFoundException("PartyMember", request.ToUserId.ToString());

        var leaders = members.Where(m => m.Role == PartyRole.Leader).ToList();
        if (!leaders.Any())
        {
            throw new Application.Exceptions.NotFoundException("PartyLeader", request.PartyId.ToString());
        }

        foreach (var leader in leaders)
        {
            if (leader.AuthUserId != newLeader.AuthUserId)
            {
                leader.Role = PartyRole.Member;
                await _memberRepository.UpdateAsync(leader, cancellationToken);
            }
        }

        newLeader.Role = PartyRole.Leader;
        await _memberRepository.UpdateAsync(newLeader, cancellationToken);
        if (_notificationService != null)
        {
            await _notificationService.SendLeadershipTransferredNotificationAsync(request.PartyId, newLeader.AuthUserId, cancellationToken);
        }

        return Unit.Value;
    }
}