using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildInvitations;

public class GetGuildInvitationsQueryHandler : IRequestHandler<GetGuildInvitationsQuery, IEnumerable<GuildInvitationDto>>
{
    private readonly IGuildInvitationRepository _guildInvitationRepository;
    private readonly IGuildRepository _guildRepository;

    public GetGuildInvitationsQueryHandler(IGuildInvitationRepository guildInvitationRepository, IGuildRepository guildRepository)
    {
        _guildInvitationRepository = guildInvitationRepository;
        _guildRepository = guildRepository;
    }

    public async Task<IEnumerable<GuildInvitationDto>> Handle(GetGuildInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _guildInvitationRepository.GetInvitationsByGuildAsync(request.GuildId, cancellationToken);
        var guilds = await _guildRepository.GetByIdsAsync(invitations.Select(x => x.GuildId).Distinct(), cancellationToken);
        var guildNameById = guilds.ToDictionary(g => g.Id, g => g.Name);

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
            ExpiresAt = i.ExpiresAt,
            GuildName = guildNameById.TryGetValue(i.GuildId, out var name) ? name : string.Empty
        });
    }
}