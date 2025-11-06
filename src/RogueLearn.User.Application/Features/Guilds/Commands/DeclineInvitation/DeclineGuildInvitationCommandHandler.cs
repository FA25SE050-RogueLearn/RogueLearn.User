using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.DeclineInvitation;

public class DeclineGuildInvitationCommandHandler : IRequestHandler<DeclineGuildInvitationCommand, Unit>
{
    private readonly IGuildInvitationRepository _invitationRepository;

    public DeclineGuildInvitationCommandHandler(IGuildInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task<Unit> Handle(DeclineGuildInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("GuildInvitation", request.InvitationId.ToString());

        if (invitation.GuildId != request.GuildId)
        {
            throw new Exceptions.BadRequestException("Invitation does not belong to target guild.");
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