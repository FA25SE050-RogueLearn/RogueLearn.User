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
        var currentLeader = members.FirstOrDefault(m => m.Role == PartyRole.Leader);
        if (currentLeader is null)
        {
            throw new Application.Exceptions.NotFoundException("PartyLeader", request.PartyId.ToString());
        }

        var newLeader = members.FirstOrDefault(m => m.AuthUserId == request.ToUserId && m.Status == MemberStatus.Active)
                        ?? throw new Application.Exceptions.NotFoundException("PartyMember", request.ToUserId.ToString());

        currentLeader.Role = PartyRole.Member;
        await _memberRepository.UpdateAsync(currentLeader, cancellationToken);

        newLeader.Role = PartyRole.Leader;
        await _memberRepository.UpdateAsync(newLeader, cancellationToken);

        return Unit.Value;
    }
}