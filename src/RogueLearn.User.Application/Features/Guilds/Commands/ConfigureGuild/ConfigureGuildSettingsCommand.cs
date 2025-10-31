using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;

public record ConfigureGuildSettingsCommand(Guid GuildId, string Name, string Description, string Privacy, int MaxMembers)
    : IRequest<Unit>;