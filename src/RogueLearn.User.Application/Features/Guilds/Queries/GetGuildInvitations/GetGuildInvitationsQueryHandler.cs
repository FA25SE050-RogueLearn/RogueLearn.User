using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildInvitations;

public class GetGuildInvitationsQueryHandler : IRequestHandler<GetGuildInvitationsQuery, IEnumerable<GuildInvitationDto>>
{
    private readonly IGuildInvitationRepository _guildInvitationRepository;
    private readonly IGuildRepository _guildRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public GetGuildInvitationsQueryHandler(IGuildInvitationRepository guildInvitationRepository, IGuildRepository guildRepository, IUserProfileRepository userProfileRepository)
    {
        _guildInvitationRepository = guildInvitationRepository;
        _guildRepository = guildRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<IEnumerable<GuildInvitationDto>> Handle(GetGuildInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _guildInvitationRepository.GetInvitationsByGuildAsync(request.GuildId, cancellationToken);
        var guilds = await _guildRepository.GetByIdsAsync(invitations.Select(x => x.GuildId).Distinct(), cancellationToken);
        var guildNameById = guilds.ToDictionary(g => g.Id, g => g.Name);

        var inviteeIds = invitations.Select(x => x.InviteeId).Distinct().ToList();
        var inviteeNameById = new Dictionary<Guid, string>();
        foreach (var id in inviteeIds)
        {
            var profile = await _userProfileRepository.GetByAuthIdAsync(id, cancellationToken);
            var name = (string.IsNullOrWhiteSpace(profile?.FirstName) && string.IsNullOrWhiteSpace(profile?.LastName))
                ? (profile?.Username ?? string.Empty)
                : $"{profile?.FirstName} {profile?.LastName}".Trim();
            inviteeNameById[id] = name;
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
            ExpiresAt = i.ExpiresAt,
            GuildName = guildNameById.TryGetValue(i.GuildId, out var name) ? name : string.Empty,
            InviteeName = inviteeNameById.TryGetValue(i.InviteeId, out var iname) ? iname : string.Empty
        });
    }
}