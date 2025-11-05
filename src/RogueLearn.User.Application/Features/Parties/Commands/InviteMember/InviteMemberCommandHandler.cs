using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.InviteMember;

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Unit>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IPartyNotificationService _notificationService;
    private readonly IUserProfileRepository _userProfileRepository;

    public InviteMemberCommandHandler(
        IPartyInvitationRepository invitationRepository,
        IPartyNotificationService notificationService,
        IUserProfileRepository userProfileRepository)
    {
        _invitationRepository = invitationRepository;
        _notificationService = notificationService;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<Unit> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        foreach (var target in request.Targets)
        {
            Guid? inviteeId = target.UserId;

            if (!inviteeId.HasValue && !string.IsNullOrWhiteSpace(target.Email))
            {
                var userProfile = await _userProfileRepository.GetByEmailAsync(target.Email, cancellationToken);
                if (userProfile != null)
                {
                    inviteeId = userProfile.AuthUserId;
                }
                else
                {
                    // TODO: Handle email invitations for non-existing users
                    continue;
                }
            }

            if (!inviteeId.HasValue)
            {
                continue;
            }

            var pending = await _invitationRepository.GetPendingInvitationsByPartyAsync(request.PartyId, cancellationToken);
            if (pending.Any(i => i.InviteeId == inviteeId.Value))
            {
                continue;
            }

            var invitation = new PartyInvitation
            {
                PartyId = request.PartyId,
                InviterId = request.InviterAuthUserId,
                InviteeId = inviteeId.Value,
                Message = request.Message,
                Status = InvitationStatus.Pending,
                ExpiresAt = request.ExpiresAt,
                InvitedAt = DateTimeOffset.UtcNow
            };

            invitation = await _invitationRepository.AddAsync(invitation, cancellationToken);

            await _notificationService.SendInvitationNotificationAsync(invitation, cancellationToken);
        }

        return Unit.Value;
    }
}