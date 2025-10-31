using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildMembers;

public class GetGuildMembersQueryHandler : IRequestHandler<GetGuildMembersQuery, IEnumerable<GuildMemberDto>>
{
    private readonly IGuildMemberRepository _guildMemberRepository;

    public GetGuildMembersQueryHandler(IGuildMemberRepository guildMemberRepository)
    {
        _guildMemberRepository = guildMemberRepository;
    }

    public async Task<IEnumerable<GuildMemberDto>> Handle(GetGuildMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _guildMemberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);
        return members.Select(m => new GuildMemberDto
        {
            MemberId = m.Id,
            GuildId = m.GuildId,
            AuthUserId = m.AuthUserId,
            Role = m.Role,
            JoinedAt = m.JoinedAt,
            LeftAt = m.LeftAt,
            Status = m.Status
        });
    }
}