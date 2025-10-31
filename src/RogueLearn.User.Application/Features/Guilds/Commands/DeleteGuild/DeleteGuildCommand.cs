using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.DeleteGuild;

public record DeleteGuildCommand(Guid GuildId) : IRequest<Unit>;