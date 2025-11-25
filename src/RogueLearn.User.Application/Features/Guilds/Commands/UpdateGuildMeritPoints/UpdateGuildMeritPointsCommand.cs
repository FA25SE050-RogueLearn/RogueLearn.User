using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.UpdateGuildMeritPoints;

public record UpdateGuildMeritPointsCommand(Guid GuildId, int PointsDelta) : IRequest<Unit>;