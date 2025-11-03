using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetMyParties;

public class GetMyPartiesQueryHandler : IRequestHandler<GetMyPartiesQuery, IReadOnlyList<PartyDto>>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly IMapper _mapper;

    public GetMyPartiesQueryHandler(IPartyRepository partyRepository, IPartyMemberRepository partyMemberRepository, IMapper mapper)
    {
        _partyRepository = partyRepository;
        _partyMemberRepository = partyMemberRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PartyDto>> Handle(GetMyPartiesQuery request, CancellationToken cancellationToken)
    {
        // Parties created by the user
        var createdParties = (await _partyRepository.GetPartiesByCreatorAsync(request.AuthUserId, cancellationToken)).ToList();

        // Parties the user belongs to (active memberships)
        var memberships = await _partyMemberRepository.GetMembershipsByUserAsync(request.AuthUserId, cancellationToken);
        var activeMembershipPartyIds = memberships
            .Where(m => m.Status == MemberStatus.Active)
            .Select(m => m.PartyId)
            .ToHashSet();

        // Fetch party details for memberships not already included in createdParties
        var createdPartyIds = createdParties.Select(p => p.Id).ToHashSet();
        var memberParties = new List<RogueLearn.User.Domain.Entities.Party>();
        foreach (var pid in activeMembershipPartyIds)
        {
            if (!createdPartyIds.Contains(pid))
            {
                var party = await _partyRepository.GetByIdAsync(pid, cancellationToken);
                if (party is not null)
                {
                    memberParties.Add(party);
                }
            }
        }

        var combined = createdParties.Concat(memberParties).Select(p => _mapper.Map<PartyDto>(p)).ToList();
        return combined;
    }
}