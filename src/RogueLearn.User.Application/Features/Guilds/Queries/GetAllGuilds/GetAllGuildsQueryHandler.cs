using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetAllGuilds;

public class GetAllGuildsQueryHandler : IRequestHandler<GetAllGuildsQuery, IEnumerable<GuildDto>>
{
    private readonly IGuildRepository _guildRepository;

    public GetAllGuildsQueryHandler(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository;
    }

    public async Task<IEnumerable<GuildDto>> Handle(GetAllGuildsQuery request, CancellationToken cancellationToken)
    {
        var all = await _guildRepository.GetAllAsync(cancellationToken);
        var filtered = request.IncludePrivate ? all : all.Where(g => g.IsPublic);

        return filtered.Select(g => new GuildDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            IsPublic = g.IsPublic,
            MaxMembers = g.MaxMembers,
            CreatedBy = g.CreatedBy,
            CreatedAt = g.CreatedAt,
            MemberCount = g.CurrentMemberCount
        });
    }
}