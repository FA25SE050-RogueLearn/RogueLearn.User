using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetMyPendingInvitations;

public class GetMyPendingInvitationsQueryHandler : IRequestHandler<GetMyPendingInvitationsQuery, IReadOnlyList<PartyInvitationDto>>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IPartyRepository _partyRepository;

    public GetMyPendingInvitationsQueryHandler(IPartyInvitationRepository invitationRepository, IPartyRepository partyRepository)
    {
        _invitationRepository = invitationRepository;
        _partyRepository = partyRepository;
    }

    public async Task<IReadOnlyList<PartyInvitationDto>> Handle(GetMyPendingInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _invitationRepository.GetPendingInvitationsByInviteeAsync(request.AuthUserId, cancellationToken);
        var parties = await _partyRepository.GetByIdsAsync(invitations.Select(x => x.PartyId).Distinct(), cancellationToken);
        var partyNameById = parties.ToDictionary(p => p.Id, p => p.Name);

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
            partyNameById.TryGetValue(i.PartyId, out var name) ? name : string.Empty
        )).ToList();
    }
}