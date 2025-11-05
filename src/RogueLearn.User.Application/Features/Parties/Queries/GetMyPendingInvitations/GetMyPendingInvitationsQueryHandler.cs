using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetMyPendingInvitations;

public class GetMyPendingInvitationsQueryHandler : IRequestHandler<GetMyPendingInvitationsQuery, IReadOnlyList<PartyInvitationDto>>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IMapper _mapper;

    public GetMyPendingInvitationsQueryHandler(IPartyInvitationRepository invitationRepository, IMapper mapper)
    {
        _invitationRepository = invitationRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PartyInvitationDto>> Handle(GetMyPendingInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _invitationRepository.GetPendingInvitationsByInviteeAsync(request.AuthUserId, cancellationToken);
        return invitations.Select(i => _mapper.Map<PartyInvitationDto>(i)).ToList();
    }
}