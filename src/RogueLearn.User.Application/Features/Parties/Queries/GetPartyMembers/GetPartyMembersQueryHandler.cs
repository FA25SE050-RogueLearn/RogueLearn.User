using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPartyMembers;

public class GetPartyMembersQueryHandler : IRequestHandler<GetPartyMembersQuery, IReadOnlyList<PartyMemberDto>>
{
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IMapper _mapper;

    public GetPartyMembersQueryHandler(
        IPartyMemberRepository partyMemberRepository,
        IUserProfileRepository userProfileRepository,
        IMapper mapper)
    {
        _partyMemberRepository = partyMemberRepository;
        _userProfileRepository = userProfileRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PartyMemberDto>> Handle(GetPartyMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _partyMemberRepository.GetMembersByPartyAsync(request.PartyId, cancellationToken);

        var results = new List<PartyMemberDto>();
        foreach (var m in members)
        {
            var profile = await _userProfileRepository.GetByAuthIdAsync(m.AuthUserId, cancellationToken);

            // Build enriched DTO with user profile info
            var dto = new PartyMemberDto(
                m.Id,
                m.PartyId,
                m.AuthUserId,
                m.Role,
                m.Status,
                m.JoinedAt,
                profile?.Username,
                profile?.Email,
                profile?.FirstName,
                profile?.LastName,
                profile?.ProfileImageUrl,
                profile?.Level ?? 0,
                profile?.ExperiencePoints ?? 0,
                profile?.Bio
            );

            results.Add(dto);
        }

        return results;
    }
}