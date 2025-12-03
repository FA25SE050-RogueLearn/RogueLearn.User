using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.DeclineInvitation;

public class DeclineGuildInvitationCommandHandler : IRequestHandler<DeclineGuildInvitationCommand, Unit>
{
    private readonly IGuildInvitationRepository _invitationRepository;
    private readonly IGuildMemberRepository? _memberRepository;
    private readonly IGuildNotificationService? _notificationService;

    public DeclineGuildInvitationCommandHandler(IGuildInvitationRepository invitationRepository, IGuildMemberRepository memberRepository, IGuildNotificationService notificationService)
    {
        _invitationRepository = invitationRepository;
        _memberRepository = memberRepository;
        _notificationService = notificationService;
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

        if (_notificationService != null && _memberRepository != null)
        {
            var members = await _memberRepository.GetMembersByGuildAsync(invitation.GuildId, cancellationToken);
            var master = members.FirstOrDefault(m => m.Status == MemberStatus.Active && m.Role == GuildRole.GuildMaster);
            if (master != null)
            {
                await _notificationService.NotifyInvitationDeclinedAsync(invitation, master.AuthUserId, cancellationToken);
            }
        }

        return Unit.Value;
    }
}