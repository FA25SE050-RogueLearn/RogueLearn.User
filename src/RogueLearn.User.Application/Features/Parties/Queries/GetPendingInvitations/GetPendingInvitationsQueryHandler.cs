using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPendingInvitations;

public class GetPendingInvitationsQueryHandler : IRequestHandler<GetPendingInvitationsQuery, IReadOnlyList<PartyInvitationDto>>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IMapper _mapper;

    public GetPendingInvitationsQueryHandler(IPartyInvitationRepository invitationRepository, IMapper mapper)
    {
        _invitationRepository = invitationRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PartyInvitationDto>> Handle(GetPendingInvitationsQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _invitationRepository.GetPendingInvitationsByPartyAsync(request.PartyId, cancellationToken);
        return invitations.Select(i => _mapper.Map<PartyInvitationDto>(i)).ToList();
    }
}