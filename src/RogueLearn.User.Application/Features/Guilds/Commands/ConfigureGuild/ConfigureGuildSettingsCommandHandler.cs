using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;

public class ConfigureGuildSettingsCommandHandler : IRequestHandler<ConfigureGuildSettingsCommand, Unit>
{
    private readonly IGuildRepository _guildRepository;

    public ConfigureGuildSettingsCommandHandler(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository;
    }

    public async Task<Unit> Handle(ConfigureGuildSettingsCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException("Guild", request.GuildId.ToString());

        guild.Name = request.Name;
        guild.Description = request.Description;
        guild.IsPublic = request.Privacy.Equals("public", StringComparison.OrdinalIgnoreCase);
        guild.MaxMembers = request.MaxMembers;
        guild.UpdatedAt = DateTimeOffset.UtcNow;

        await _guildRepository.UpdateAsync(guild, cancellationToken);
        return Unit.Value;
    }
}