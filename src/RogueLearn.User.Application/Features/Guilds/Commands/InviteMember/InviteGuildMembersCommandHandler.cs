using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;

public class InviteGuildMembersCommandHandler : IRequestHandler<InviteGuildMembersCommand, InviteGuildMembersResponse>
{
    private readonly IGuildInvitationRepository _invitationRepository;

    public InviteGuildMembersCommandHandler(IGuildInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task<InviteGuildMembersResponse> Handle(InviteGuildMembersCommand request, CancellationToken cancellationToken)
    {
        var createdIds = new List<Guid>();
        foreach (var target in request.Targets)
        {
            if (target.UserId is Guid inviteeId)
            {
                // Idempotency: if an active pending invite exists for same guild & user, skip
                var pending = await _invitationRepository.GetPendingInvitationsByGuildAsync(request.GuildId, cancellationToken);
                if (pending.Any(i => i.InviteeId == inviteeId))
                {
                    continue;
                }

                var invitation = new GuildInvitation
                {
                    GuildId = request.GuildId,
                    InviterId = request.InviterAuthUserId,
                    InviteeId = inviteeId,
                    InvitationType = InvitationType.Invite,
                    Status = InvitationStatus.Pending,
                    Message = request.Message,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
                };

                invitation = await _invitationRepository.AddAsync(invitation, cancellationToken);
                createdIds.Add(invitation.Id);
            }
            else if (!string.IsNullOrWhiteSpace(target.Email))
            {
                // TODO: support email invitations via Notification service or external mailer
                // For now, skip email invites in core implementation.
                continue;
            }
        }

        return new InviteGuildMembersResponse(createdIds);
    }
}