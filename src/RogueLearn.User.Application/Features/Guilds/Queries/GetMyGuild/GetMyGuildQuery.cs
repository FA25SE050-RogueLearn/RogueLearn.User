using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuild;

public record GetMyGuildQuery(Guid AuthUserId) : IRequest<GuildDto?>;