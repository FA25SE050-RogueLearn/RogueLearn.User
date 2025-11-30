using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;

public class ConfigureGuildSettingsCommandHandler : IRequestHandler<ConfigureGuildSettingsCommand, Unit>
{
    private readonly IGuildRepository _guildRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public ConfigureGuildSettingsCommandHandler(IGuildRepository guildRepository, IRoleRepository roleRepository, IUserRoleRepository userRoleRepository)
    {
        _guildRepository = guildRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<Unit> Handle(ConfigureGuildSettingsCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException("Guild", request.GuildId.ToString());

        // Validate that the new max is strictly greater than current members
        if (request.MaxMembers <= guild.CurrentMemberCount)
        {
            throw new Application.Exceptions.BadRequestException("Max members must be greater than the current member count.");
        }

        // Role-based cap: Verified Lecturer can set up to 100, otherwise up to 50
        var verifiedLecturerRole = await _roleRepository.GetByNameAsync("Verified Lecturer", cancellationToken);
        var userRoles = await _userRoleRepository.GetRolesForUserAsync(request.ActorAuthUserId, cancellationToken);
        var isVerifiedLecturer = verifiedLecturerRole != null && userRoles.Any(ur => ur.RoleId == verifiedLecturerRole.Id);
        var maxAllowed = isVerifiedLecturer ? 100 : 50;

        if (!isVerifiedLecturer && guild.CurrentMemberCount >= 50)
        {
            throw new Application.Exceptions.UnprocessableEntityException("Guild settings are locked until a Verified Lecturer is GuildMaster.");
        }
        if (request.MaxMembers > maxAllowed)
        {
            throw new Application.Exceptions.BadRequestException($"Max guild members cannot exceed {maxAllowed} for your role.");
        }

        guild.Name = request.Name;
        guild.Description = request.Description;
        guild.IsPublic = request.Privacy.Equals("public", StringComparison.OrdinalIgnoreCase);
        guild.MaxMembers = request.MaxMembers;
        guild.UpdatedAt = DateTimeOffset.UtcNow;

        await _guildRepository.UpdateAsync(guild, cancellationToken);
        return Unit.Value;
    }
}