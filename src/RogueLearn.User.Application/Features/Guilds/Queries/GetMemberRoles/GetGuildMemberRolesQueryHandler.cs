using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetMemberRoles;

public class GetGuildMemberRolesQueryHandler : IRequestHandler<GetGuildMemberRolesQuery, IReadOnlyList<GuildRole>>
{
    private readonly IGuildMemberRepository _guildMemberRepository;

    public GetGuildMemberRolesQueryHandler(IGuildMemberRepository guildMemberRepository)
    {
        _guildMemberRepository = guildMemberRepository;
    }

    public async Task<IReadOnlyList<GuildRole>> Handle(GetGuildMemberRolesQuery request, CancellationToken cancellationToken)
    {
        var member = await _guildMemberRepository.GetMemberAsync(request.GuildId, request.MemberAuthUserId, cancellationToken);
        if (member is null)
        {
            return Array.Empty<GuildRole>();
        }
        // Current data model supports a single role; wrap in list for compatibility
        return new List<GuildRole> { member.Role };
    }
}