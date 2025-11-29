using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPendingInvitations;

public class GetPendingInvitationsQueryHandler : IRequestHandler<GetPendingInvitationsQuery, IReadOnlyList<PartyInvitationDto>>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public GetPendingInvitationsQueryHandler(IPartyInvitationRepository invitationRepository, IPartyRepository partyRepository, IUserProfileRepository userProfileRepository)
    {
        _invitationRepository = invitationRepository;
        _partyRepository = partyRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<IReadOnlyList<PartyInvitationDto>> Handle(GetPendingInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _invitationRepository.GetPendingInvitationsByPartyAsync(request.PartyId, cancellationToken);
        var parties = await _partyRepository.GetByIdsAsync(invitations.Select(x => x.PartyId).Distinct(), cancellationToken);
        var partyNameById = parties.ToDictionary(p => p.Id, p => p.Name);

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

        return invitations.Select(i => new PartyInvitationDto(
            i.Id,
            i.PartyId,
            i.InviterId,
            i.InviteeId,
            i.Status,
            i.Message,
            i.InvitedAt,
            i.RespondedAt,
            i.ExpiresAt,
            partyNameById.TryGetValue(i.PartyId, out var name) ? name : string.Empty,
            inviteeNameById.TryGetValue(i.InviteeId, out var iname) ? iname : string.Empty
        )).ToList();
    }
}