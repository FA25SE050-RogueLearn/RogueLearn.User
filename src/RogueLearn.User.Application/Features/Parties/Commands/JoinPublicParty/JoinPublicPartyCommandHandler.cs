using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.JoinPublicParty;

public class JoinPublicPartyCommandHandler : IRequestHandler<JoinPublicPartyCommand, Unit>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IPartyMemberRepository _memberRepository;
    private readonly IPartyInvitationRepository? _invitationRepository;

    public JoinPublicPartyCommandHandler(IPartyRepository partyRepository, IPartyMemberRepository memberRepository)
    {
        _partyRepository = partyRepository;
        _memberRepository = memberRepository;
        _invitationRepository = null;
    }

    public JoinPublicPartyCommandHandler(IPartyRepository partyRepository, IPartyMemberRepository memberRepository, IPartyInvitationRepository invitationRepository)
    {
        _partyRepository = partyRepository;
        _memberRepository = memberRepository;
        _invitationRepository = invitationRepository;
    }

    public async Task<Unit> Handle(JoinPublicPartyCommand request, CancellationToken cancellationToken)
    {
        var party = await _partyRepository.GetByIdAsync(request.PartyId, cancellationToken);
        if (party is null)
        {
            throw new BadRequestException("Party not found.");
        }

        if (!party.IsPublic)
        {
            throw new BadRequestException("This party is not public. You must be invited by a leader.");
        }

        var activeCount = await _memberRepository.CountActiveMembersAsync(request.PartyId, cancellationToken);
        if (activeCount >= party.MaxMembers)
        {
            throw new BadRequestException("Party is at maximum capacity.");
        }

        var existing = await _memberRepository.GetMemberAsync(request.PartyId, request.AuthUserId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status == MemberStatus.Active)
            {
                throw new BadRequestException("User is already an active member of this party.");
            }

            existing.Status = MemberStatus.Active;
            existing.JoinedAt = DateTimeOffset.UtcNow;
            await _memberRepository.UpdateAsync(existing, cancellationToken);
            if (_invitationRepository != null)
            {
                var pendingInvites = await _invitationRepository.GetPendingInvitationsByInviteeAsync(request.AuthUserId, cancellationToken);
                var toDecline = pendingInvites.Where(i => i.PartyId == request.PartyId && i.Status == InvitationStatus.Pending).ToList();
                if (toDecline.Any())
                {
                    foreach (var inv in toDecline)
                    {
                        inv.Status = InvitationStatus.Declined;
                        inv.RespondedAt = DateTimeOffset.UtcNow;
                    }
                    await _invitationRepository.UpdateRangeAsync(toDecline, cancellationToken);
                }
            }
            return Unit.Value;
        }

        var member = new PartyMember
        {
            PartyId = request.PartyId,
            AuthUserId = request.AuthUserId,
            Role = PartyRole.Member,
            Status = MemberStatus.Active,
            JoinedAt = DateTimeOffset.UtcNow
        };

        await _memberRepository.AddAsync(member, cancellationToken);
        if (_invitationRepository != null)
        {
            var pendingInvites = await _invitationRepository.GetPendingInvitationsByInviteeAsync(request.AuthUserId, cancellationToken);
            var toDecline = pendingInvites.Where(i => i.PartyId == request.PartyId && i.Status == InvitationStatus.Pending).ToList();
            if (toDecline.Any())
            {
                foreach (var inv in toDecline)
                {
                    inv.Status = InvitationStatus.Declined;
                    inv.RespondedAt = DateTimeOffset.UtcNow;
                }
                await _invitationRepository.UpdateRangeAsync(toDecline, cancellationToken);
            }
        }
        return Unit.Value;
    }
}