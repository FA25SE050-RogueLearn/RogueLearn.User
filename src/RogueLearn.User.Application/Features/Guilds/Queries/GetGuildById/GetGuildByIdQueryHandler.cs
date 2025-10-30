using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildById;

public class GetGuildByIdQueryHandler : IRequestHandler<GetGuildByIdQuery, GuildDto>
{
    private readonly IGuildRepository _guildRepository;

    public GetGuildByIdQueryHandler(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository;
    }

    public async Task<GuildDto> Handle(GetGuildByIdQuery request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new NotFoundException("Guild", request.GuildId.ToString());

        return new GuildDto
        {
            Id = guild.Id,
            Name = guild.Name,
            Description = guild.Description,
            IsPublic = guild.IsPublic,
            MaxMembers = guild.MaxMembers,
            CreatedAt = guild.CreatedAt,
            CreatedBy = guild.CreatedBy,
        };
    }
}