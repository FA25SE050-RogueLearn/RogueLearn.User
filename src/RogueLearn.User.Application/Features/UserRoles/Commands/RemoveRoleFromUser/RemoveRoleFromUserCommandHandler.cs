using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand>
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IGuildMemberRepository? _guildMemberRepository;
    private readonly IGuildRepository? _guildRepository;
    private readonly ILogger<RemoveRoleFromUserCommandHandler> _logger;

    public RemoveRoleFromUserCommandHandler(
        IUserRoleRepository userRoleRepository,
        IUserProfileRepository userProfileRepository,
        IRoleRepository roleRepository,
        IGuildMemberRepository guildMemberRepository,
        IGuildRepository guildRepository,
        ILogger<RemoveRoleFromUserCommandHandler> logger)
    {
        _userRoleRepository = userRoleRepository;
        _userProfileRepository = userProfileRepository;
        _roleRepository = roleRepository;
        _guildMemberRepository = guildMemberRepository;
        _guildRepository = guildRepository;
        _logger = logger;
    }

    public RemoveRoleFromUserCommandHandler(
        IUserRoleRepository userRoleRepository,
        IUserProfileRepository userProfileRepository,
        IRoleRepository roleRepository,
        ILogger<RemoveRoleFromUserCommandHandler> logger)
    {
        _userRoleRepository = userRoleRepository;
        _userProfileRepository = userProfileRepository;
        _roleRepository = roleRepository;
        _guildMemberRepository = null;
        _guildRepository = null;
        _logger = logger;
    }

    public async Task Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", request.AuthUserId);
        }

        // Verify role exists
        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role == null)
        {
            throw new NotFoundException("Role", request.RoleId);
        }

        // Find the user role to remove
        var existingUserRoles = await _userRoleRepository.GetRolesForUserAsync(request.AuthUserId, cancellationToken);
        var userRoleToRemove = existingUserRoles.FirstOrDefault(ur => ur.RoleId == request.RoleId);
        
        if (userRoleToRemove == null)
        {
            return; // idempotent: nothing to remove
        }

        await _userRoleRepository.DeleteAsync(userRoleToRemove.Id, cancellationToken);

        if (role.Name.Equals("Verified Lecturer", StringComparison.OrdinalIgnoreCase) && _guildMemberRepository != null && _guildRepository != null)
        {
            var now = DateTimeOffset.UtcNow;
            var memberships = await _guildMemberRepository.GetMembershipsByUserAsync(request.AuthUserId, cancellationToken);
            var masterGuildIds = memberships
                .Where(m => m.Status == MemberStatus.Active && m.Role == GuildRole.GuildMaster)
                .Select(m => m.GuildId)
                .Distinct()
                .ToList();

            foreach (var gid in masterGuildIds)
            {
                var guild = await _guildRepository.GetByIdAsync(gid, cancellationToken);
                if (guild != null)
                {
                    var activeCount = await _guildMemberRepository.CountActiveMembersAsync(gid, cancellationToken);
                    guild.MaxMembers = activeCount > 50 ? activeCount : 50;
                    guild.UpdatedAt = now;
                    guild.IsLecturerGuild = false;
                    await _guildRepository.UpdateAsync(guild, cancellationToken);
                }
            }
        }

        _logger.LogInformation("Role '{RoleName}' removed from auth user {AuthUserId}", role.Name, request.AuthUserId);
    }
}