using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetMemberRoles;

public class GetPartyMemberRolesQueryHandler : IRequestHandler<GetPartyMemberRolesQuery, IReadOnlyList<PartyRole>>
{
    private readonly IPartyMemberRepository _partyMemberRepository;

    public GetPartyMemberRolesQueryHandler(IPartyMemberRepository partyMemberRepository)
    {
        _partyMemberRepository = partyMemberRepository;
    }

    public async Task<IReadOnlyList<PartyRole>> Handle(GetPartyMemberRolesQuery request, CancellationToken cancellationToken)
    {
        var member = await _partyMemberRepository.GetMemberAsync(request.PartyId, request.MemberAuthUserId, cancellationToken);
        if (member is null)
        {
            return Array.Empty<PartyRole>();
        }
        return new List<PartyRole> { member.Role };
    }
}