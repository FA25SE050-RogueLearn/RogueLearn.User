using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildInvitations;

public class GetGuildInvitationsQueryHandler : IRequestHandler<GetGuildInvitationsQuery, IEnumerable<GuildInvitationDto>>
{
    private readonly IGuildInvitationRepository _guildInvitationRepository;

    public GetGuildInvitationsQueryHandler(IGuildInvitationRepository guildInvitationRepository)
    {
        _guildInvitationRepository = guildInvitationRepository;
    }

    public async Task<IEnumerable<GuildInvitationDto>> Handle(GetGuildInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _guildInvitationRepository.GetInvitationsByGuildAsync(request.GuildId, cancellationToken);
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
        });
    }
}