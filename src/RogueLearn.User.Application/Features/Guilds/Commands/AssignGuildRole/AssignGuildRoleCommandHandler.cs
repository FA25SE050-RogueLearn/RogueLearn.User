using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ManageRoles;

public class AssignGuildRoleCommandHandler : IRequestHandler<AssignGuildRoleCommand, Unit>
{
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly ILogger<AssignGuildRoleCommandHandler> _logger;
    private readonly IGuildNotificationService? _notificationService;

    public AssignGuildRoleCommandHandler(IGuildMemberRepository guildMemberRepository, ILogger<AssignGuildRoleCommandHandler> logger)
    {
        _guildMemberRepository = guildMemberRepository;
        _logger = logger;
        _notificationService = null;
    }

    // Convenience overload for tests that don't pass a logger
    public AssignGuildRoleCommandHandler(IGuildMemberRepository guildMemberRepository)
        : this(guildMemberRepository, LoggerFactory.Create(builder => { }).CreateLogger<AssignGuildRoleCommandHandler>())
    {
    }

    public AssignGuildRoleCommandHandler(IGuildMemberRepository guildMemberRepository, ILogger<AssignGuildRoleCommandHandler> logger, IGuildNotificationService notificationService)
    {
        _guildMemberRepository = guildMemberRepository;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(AssignGuildRoleCommand request, CancellationToken cancellationToken)
    {
        // Authorization: GuildMaster of the guild OR Platform Admin (handled at controller level)
        if (!request.IsAdminOverride)
        {
            var isMaster = await _guildMemberRepository.IsGuildMasterAsync(request.GuildId, request.ActorAuthUserId, cancellationToken);
            if (!isMaster)
            {
                throw new ForbiddenException("Only GuildMaster may assign roles in this guild.");
            }
        }

        var member = await _guildMemberRepository.GetMemberAsync(request.GuildId, request.MemberAuthUserId, cancellationToken)
            ?? throw new NotFoundException("GuildMember", request.MemberAuthUserId.ToString());

        // Safeguard: cannot assign GuildMaster via role management; use transfer leadership endpoint
        if (request.RoleToAssign == GuildRole.GuildMaster)
        {
            throw new UnprocessableEntityException("Cannot assign GuildMaster via role management. Use transfer leadership endpoint.");
        }

        // Idempotency: no-op if same role
        if (member.Role == request.RoleToAssign)
        {
            return Unit.Value; // 204 No Content expected by controller
        }

        var oldRole = member.Role;
        member.Role = request.RoleToAssign;
        await _guildMemberRepository.UpdateAsync(member, cancellationToken);

        _logger.LogInformation("Guild role changed: GuildId={GuildId}, MemberAuthUserId={MemberId}, ActorAuthUserId={ActorId}, OldRole={OldRole}, NewRole={NewRole}, AdminOverride={AdminOverride}",
            request.GuildId, request.MemberAuthUserId, request.ActorAuthUserId, oldRole, request.RoleToAssign, request.IsAdminOverride);

        if (_notificationService != null)
        {
            await _notificationService.NotifyRoleAssignedAsync(request.GuildId, request.MemberAuthUserId, request.RoleToAssign, cancellationToken);
        }

        return Unit.Value;
    }
}