using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildById;

public record GetGuildByIdQuery(Guid GuildId) : IRequest<GuildDto>;