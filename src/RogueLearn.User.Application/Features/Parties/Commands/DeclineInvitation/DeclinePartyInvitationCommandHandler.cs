using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.DeclineInvitation;

public class DeclinePartyInvitationCommandHandler : IRequestHandler<DeclinePartyInvitationCommand, Unit>
{
    private readonly IPartyInvitationRepository _invitationRepository;

    public DeclinePartyInvitationCommandHandler(IPartyInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
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

        return Unit.Value;
    }
}