using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.DeclineInvitation;

public class DeclinePartyInvitationCommandHandler : IRequestHandler<DeclinePartyInvitationCommand, Unit>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IPartyNotificationService? _notificationService;

    public DeclinePartyInvitationCommandHandler(IPartyInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
        _notificationService = null;
    }

    public DeclinePartyInvitationCommandHandler(IPartyInvitationRepository invitationRepository, IPartyNotificationService notificationService)
    {
        _invitationRepository = invitationRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(DeclinePartyInvitationCommand request, CancellationToken cancellationToken)
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

        invitation.Status = InvitationStatus.Declined;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        if (_notificationService != null)
        {
            await _notificationService.SendInvitationDeclinedNotificationAsync(invitation, cancellationToken);
        }

        return Unit.Value;
    }
}