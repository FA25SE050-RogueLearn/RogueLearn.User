using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.LeaveParty;

public class LeavePartyCommandHandler : IRequestHandler<LeavePartyCommand, Unit>
{
    private readonly IPartyMemberRepository _memberRepository;
    private readonly IPartyRepository _partyRepository;

    public LeavePartyCommandHandler(IPartyMemberRepository memberRepository, IPartyRepository partyRepository)
    {
        _memberRepository = memberRepository;
        _partyRepository = partyRepository;
    }

    public async Task<Unit> Handle(LeavePartyCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetMemberAsync(request.PartyId, request.AuthUserId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("PartyMember", request.AuthUserId.ToString());

        if (member.Role == PartyRole.Leader)
        {
            var activeCount = await _memberRepository.CountActiveMembersAsync(request.PartyId, cancellationToken);
            if (activeCount > 1)
            {
                // Transfer ownership to the next highest role (Member)
                var members = await _memberRepository.GetMembersByPartyAsync(request.PartyId, cancellationToken);
                var eligible = members
                    .Where(m => m.Status == MemberStatus.Active && m.AuthUserId != request.AuthUserId)
                    .ToList();

                var nextOwner = eligible
                    .Where(m => m.Role == PartyRole.Member)
                    .OrderBy(m => m.JoinedAt)
                    .FirstOrDefault();

                if (nextOwner == null)
                {
                    // Fallback: pick any active member (should not happen when activeCount > 1)
                    nextOwner = eligible.OrderBy(m => m.JoinedAt).FirstOrDefault();
                }

                if (nextOwner == null)
                {
                    // No eligible successor found; delete leaving member then delete party
                    await _memberRepository.DeleteAsync(member.Id, cancellationToken);
                    await _partyRepository.DeleteAsync(request.PartyId, cancellationToken);
                    return Unit.Value;
                }

                nextOwner.Role = PartyRole.Leader;
                await _memberRepository.UpdateAsync(nextOwner, cancellationToken);
            }
            else
            {
                // Sole member is the leader; delete leaving member then delete the party
                await _memberRepository.DeleteAsync(member.Id, cancellationToken);
                await _partyRepository.DeleteAsync(request.PartyId, cancellationToken);
                return Unit.Value;
            }
        }

        // Remove the leaving member from the party
        await _memberRepository.DeleteAsync(member.Id, cancellationToken);
        return Unit.Value;
    }
}