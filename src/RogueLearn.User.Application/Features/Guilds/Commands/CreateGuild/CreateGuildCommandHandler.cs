using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.CreateGuild;

public class CreateGuildCommandHandler : IRequestHandler<CreateGuildCommand, CreateGuildResponse>
{
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;

    public CreateGuildCommandHandler(IGuildRepository guildRepository, IGuildMemberRepository guildMemberRepository, IUserRoleRepository userRoleRepository, IRoleRepository roleRepository)
    {
        _guildRepository = guildRepository;
        _guildMemberRepository = guildMemberRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
    }

    public async Task<CreateGuildResponse> Handle(CreateGuildCommand request, CancellationToken cancellationToken)
    {
        // Enforce one-guild-per-user constraints
        // 1) If the user is already an active member of any guild, they cannot create another guild
        var existingMemberships = await _guildMemberRepository.GetMembershipsByUserAsync(request.CreatorAuthUserId, cancellationToken);
        if (existingMemberships.Any(m => m.Status == MemberStatus.Active))
        {
            throw new Exceptions.BadRequestException("User already belongs to a guild and cannot create another guild.");
        }

        // 2) If the user has already created a guild, they cannot create another guild
        var createdGuilds = await _guildRepository.GetGuildsByCreatorAsync(request.CreatorAuthUserId, cancellationToken);
        if (createdGuilds.Any())
        {
            throw new Exceptions.BadRequestException("User has already created a guild and cannot create another.");
        }

        // Role-based max members constraints
        var verifiedLecturerRole = await _roleRepository.GetByNameAsync("Verified Lecturer", cancellationToken);
        var userRoles = await _userRoleRepository.GetRolesForUserAsync(request.CreatorAuthUserId, cancellationToken);
        var isVerifiedLecturer = verifiedLecturerRole != null && userRoles.Any(ur => ur.RoleId == verifiedLecturerRole.Id);
        var maxAllowed = isVerifiedLecturer ? 100 : 50;
        if (request.MaxMembers > maxAllowed)
        {
            throw new Exceptions.BadRequestException($"Max guild members cannot exceed {maxAllowed} for your role.");
        }

        // Ensure max members is greater than current members (creator counts as 1)
        if (request.MaxMembers <= 1)
        {
            throw new Exceptions.BadRequestException("Max members must be greater than the current member count (1).");
        }

        var isPublic = request.Privacy.Equals("public", StringComparison.OrdinalIgnoreCase);

        var guild = new Guild
        {
            Name = request.Name,
            Description = request.Description,
            GuildType = GuildType.Study,
            MaxMembers = request.MaxMembers,
            IsPublic = isPublic,
            IsLecturerGuild = isVerifiedLecturer,
            CreatedBy = request.CreatorAuthUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CurrentMemberCount = 1
        };

        guild = await _guildRepository.AddAsync(guild, cancellationToken);

        var master = new GuildMember
        {
            GuildId = guild.Id,
            AuthUserId = request.CreatorAuthUserId,
            Role = GuildRole.GuildMaster,
            Status = MemberStatus.Active,
            JoinedAt = DateTimeOffset.UtcNow
        };
        await _guildMemberRepository.AddAsync(master, cancellationToken);

        // Assign the global "Guild Master" role to the user who created the guild
        var guildMasterRole = await _roleRepository.GetByNameAsync("Guild Master", cancellationToken);
        if (guildMasterRole == null)
        {
            // If the role is not configured, surface a clear error so the environment can be corrected
            throw new Exceptions.NotFoundException("Role", "Guild Master");
        }

        // Avoid duplicate assignment if the user already has the role
        var existingUserRoles = await _userRoleRepository.GetRolesForUserAsync(request.CreatorAuthUserId, cancellationToken);
        if (!existingUserRoles.Any(ur => ur.RoleId == guildMasterRole.Id))
        {
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                AuthUserId = request.CreatorAuthUserId,
                RoleId = guildMasterRole.Id,
                AssignedAt = DateTimeOffset.UtcNow,
                AssignedBy = request.CreatorAuthUserId
            };

            await _userRoleRepository.AddAsync(userRole, cancellationToken);
        }

        var dto = new GuildDto
        {
            Id = guild.Id,
            Name = guild.Name,
            Description = guild.Description,
            IsPublic = guild.IsPublic,
            IsLecturerGuild = guild.IsLecturerGuild,
            MaxMembers = guild.MaxMembers,
            CreatedBy = guild.CreatedBy,
            CreatedAt = guild.CreatedAt,
            MemberCount = guild.CurrentMemberCount
        };

        return new CreateGuildResponse
        {
            GuildId = guild.Id,
            RoleGranted = "GuildMaster",
            Guild = dto
        };
    }
}