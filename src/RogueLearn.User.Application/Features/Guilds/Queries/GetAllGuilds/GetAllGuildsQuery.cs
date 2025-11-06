using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetAllGuilds;

public record GetAllGuildsQuery(bool IncludePrivate = false) : IRequest<IEnumerable<GuildDto>>;