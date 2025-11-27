using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetAllGuilds;

public record GetAllGuildsFullQuery(bool IncludePrivate = true) : IRequest<IReadOnlyList<GuildFullDto>>;
