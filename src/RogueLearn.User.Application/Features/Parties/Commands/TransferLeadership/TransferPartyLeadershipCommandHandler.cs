using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.TransferLeadership;

public class TransferPartyLeadershipCommandHandler : IRequestHandler<TransferPartyLeadershipCommand, Unit>
{
    private readonly IPartyMemberRepository _memberRepository;

    public TransferPartyLeadershipCommandHandler(IPartyMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
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

        return Unit.Value;
    }
}