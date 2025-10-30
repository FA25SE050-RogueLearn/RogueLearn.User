using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuild;

public class GetMyGuildQueryHandler : IRequestHandler<GetMyGuildQuery, GuildDto?>
{
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildMemberRepository _guildMemberRepository;

    public GetMyGuildQueryHandler(IGuildRepository guildRepository, IGuildMemberRepository guildMemberRepository)
    {
        _guildRepository = guildRepository;
        _guildMemberRepository = guildMemberRepository;
    }

    public async Task<GuildDto?> Handle(GetMyGuildQuery request, CancellationToken cancellationToken)
    {
        var memberships = await _guildMemberRepository.GetMembershipsByUserAsync(request.AuthUserId, cancellationToken);
        var membership = memberships.FirstOrDefault(m => m.Status == Domain.Enums.MemberStatus.Active);
        if (membership == null)
        {
            return null;
        }

        var guild = await _guildRepository.GetByIdAsync(membership.GuildId, cancellationToken);
        if (guild == null)
        {
            return null;
        }

        var activeCount = await _guildMemberRepository.CountActiveMembersAsync(guild.Id, cancellationToken);

        return new GuildDto
        {
            Id = guild.Id,
            Name = guild.Name,
            Description = guild.Description,
            IsPublic = guild.IsPublic,
            MaxMembers = guild.MaxMembers,
            CreatedAt = guild.CreatedAt,
            CreatedBy = guild.CreatedBy,
            MemberCount = activeCount,
        };
    }
}