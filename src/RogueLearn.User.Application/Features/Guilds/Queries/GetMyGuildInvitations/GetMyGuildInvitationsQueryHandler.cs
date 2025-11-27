using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuildInvitations;

public class GetMyGuildInvitationsQueryHandler : IRequestHandler<GetMyGuildInvitationsQuery, IReadOnlyList<GuildInvitationDto>>
{
    private readonly IGuildInvitationRepository _guildInvitationRepository;

    public GetMyGuildInvitationsQueryHandler(IGuildInvitationRepository guildInvitationRepository)
    {
        _guildInvitationRepository = guildInvitationRepository;
    }

    public async Task<IReadOnlyList<GuildInvitationDto>> Handle(GetMyGuildInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _guildInvitationRepository.FindAsync(i => i.InviteeId == request.AuthUserId, cancellationToken);

        if (request.PendingOnly)
        {
            invitations = invitations.Where(i => i.Status == RogueLearn.User.Domain.Enums.InvitationStatus.Pending);
        }

        return invitations.Select(i => new GuildInvitationDto
        {
            InvitationId = i.Id,
            GuildId = i.GuildId,
            InviterAuthUserId = i.InviterId.GetValueOrDefault(),
            TargetUserId = i.InviteeId,
            TargetEmail = null,
            Message = i.Message,
            Status = i.Status,
            CreatedAt = i.CreatedAt,
            RespondedAt = i.RespondedAt,
            ExpiresAt = i.ExpiresAt
        }).ToList();
    }
}