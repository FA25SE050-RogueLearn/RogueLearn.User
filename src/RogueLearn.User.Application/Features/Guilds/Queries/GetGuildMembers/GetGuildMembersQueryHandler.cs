using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildMembers;

public class GetGuildMembersQueryHandler : IRequestHandler<GetGuildMembersQuery, IEnumerable<GuildMemberDto>>
{
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public GetGuildMembersQueryHandler(IGuildMemberRepository guildMemberRepository, IUserProfileRepository userProfileRepository)
    {
        _guildMemberRepository = guildMemberRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<IEnumerable<GuildMemberDto>> Handle(GetGuildMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _guildMemberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);

        var results = new List<GuildMemberDto>();
        foreach (var m in members)
        {
            var profile = await _userProfileRepository.GetByAuthIdAsync(m.AuthUserId, cancellationToken);

            results.Add(new GuildMemberDto
            {
                MemberId = m.Id,
                GuildId = m.GuildId,
                AuthUserId = m.AuthUserId,
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                LeftAt = m.LeftAt,
                Status = m.Status,
                Username = profile?.Username,
                Email = profile?.Email,
                FirstName = profile?.FirstName,
                LastName = profile?.LastName,
                ProfileImageUrl = profile?.ProfileImageUrl,
                Level = profile?.Level ?? 0,
                ExperiencePoints = profile?.ExperiencePoints ?? 0,
                Bio = profile?.Bio
            });
        }

        return results;
    }
}