using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;

public record ConfigureGuildSettingsCommand(Guid GuildId, Guid ActorAuthUserId, string Name, string Description, string Privacy, int MaxMembers)
    : IRequest<Unit>;