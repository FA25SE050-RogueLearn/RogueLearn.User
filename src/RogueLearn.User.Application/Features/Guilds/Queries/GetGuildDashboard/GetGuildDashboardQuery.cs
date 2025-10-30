using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildDashboard;

public record GetGuildDashboardQuery(Guid GuildId) : IRequest<GuildDashboardDto>;