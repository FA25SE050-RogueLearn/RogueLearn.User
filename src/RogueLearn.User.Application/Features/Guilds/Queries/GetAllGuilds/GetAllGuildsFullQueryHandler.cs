using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetAllGuilds;

public class GetAllGuildsFullQueryHandler : IRequestHandler<GetAllGuildsFullQuery, IReadOnlyList<GuildFullDto>>
{
    private readonly IGuildRepository _guildRepository;

    public GetAllGuildsFullQueryHandler(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository;
    }

    public async Task<IReadOnlyList<GuildFullDto>> Handle(GetAllGuildsFullQuery request, CancellationToken cancellationToken)
    {
        var all = await _guildRepository.GetAllAsync(cancellationToken);
        // var filtered = request.IncludePrivate ? all : all.Where(g => g.IsPublic);

        return all.Select(g => new GuildFullDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            GuildType = g.GuildType,
            MaxMembers = g.MaxMembers,
            CurrentMemberCount = g.CurrentMemberCount,
            MeritPoints = g.MeritPoints,
            IsPublic = g.IsPublic,
            IsLecturerGuild = g.IsLecturerGuild,
            RequiresApproval = g.RequiresApproval,
            BannerImageUrl = g.BannerImageUrl,
            CreatedBy = g.CreatedBy,
            CreatedAt = g.CreatedAt,
            UpdatedAt = g.UpdatedAt
        }).ToList();
    }
}