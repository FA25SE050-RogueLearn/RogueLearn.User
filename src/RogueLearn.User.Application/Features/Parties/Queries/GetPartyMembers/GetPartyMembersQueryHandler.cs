using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPartyMembers;

public class GetPartyMembersQueryHandler : IRequestHandler<GetPartyMembersQuery, IReadOnlyList<PartyMemberDto>>
{
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly IMapper _mapper;

    public GetPartyMembersQueryHandler(IPartyMemberRepository partyMemberRepository, IMapper mapper)
    {
        _partyMemberRepository = partyMemberRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PartyMemberDto>> Handle(GetPartyMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _partyMemberRepository.GetMembersByPartyAsync(request.PartyId, cancellationToken);
        return members.Select(m => _mapper.Map<PartyMemberDto>(m)).ToList();
    }
}