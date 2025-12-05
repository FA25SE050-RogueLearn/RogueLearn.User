using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.AcceptInvitation;

public class AcceptPartyInvitationCommandHandler : IRequestHandler<AcceptPartyInvitationCommand, Unit>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IPartyMemberRepository _memberRepository;
    private readonly IPartyNotificationService? _notificationService;

    public AcceptPartyInvitationCommandHandler(IPartyInvitationRepository invitationRepository, IPartyMemberRepository memberRepository, IPartyNotificationService notificationService)
    {
        _invitationRepository = invitationRepository;
        _memberRepository = memberRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(AcceptPartyInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("PartyInvitation", request.InvitationId.ToString());

        if (invitation.PartyId != request.PartyId)
        {
            throw new Exceptions.BadRequestException("Invitation does not belong to target party.");
        }

        if (invitation.Status != InvitationStatus.Pending || invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new Exceptions.BadRequestException("Invitation is not valid.");
        }

        if (invitation.InviteeId != request.AuthUserId)
        {
            throw new Exceptions.ForbiddenException("Invitation not intended for this user.");
        }

        var existingMember = await _memberRepository.GetMemberAsync(request.PartyId, request.AuthUserId, cancellationToken);
        if (existingMember != null && existingMember.Status == MemberStatus.Active)
        {
            throw new Exceptions.BadRequestException("User is already a member of this party.");
        }

        if (existingMember != null)
        {
            existingMember.Status = MemberStatus.Active;
            existingMember.JoinedAt = DateTimeOffset.UtcNow;
            existingMember.LeftAt = null;
            await _memberRepository.UpdateAsync(existingMember, cancellationToken);
        }
        else
        {
            var member = new PartyMember
            {
                PartyId = request.PartyId,
                AuthUserId = request.AuthUserId,
                Role = PartyRole.Member,
                Status = MemberStatus.Active,
                JoinedAt = DateTimeOffset.UtcNow
            };
            await _memberRepository.AddAsync(member, cancellationToken);
        }

        invitation.Status = InvitationStatus.Accepted;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        if (_notificationService != null)
        {
            await _notificationService.SendInvitationAcceptedNotificationAsync(invitation, cancellationToken);
        }

        return Unit.Value;
    }
}