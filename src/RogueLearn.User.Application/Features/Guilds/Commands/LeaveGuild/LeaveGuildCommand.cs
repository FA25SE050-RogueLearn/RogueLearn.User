using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.LeaveGuild;

public record LeaveGuildCommand(Guid GuildId, Guid AuthUserId) : IRequest<Unit>;