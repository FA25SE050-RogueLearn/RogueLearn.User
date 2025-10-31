using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ManageRoles;

public class RevokeGuildRoleCommandHandler : IRequestHandler<RevokeGuildRoleCommand, Unit>
{
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly ILogger<RevokeGuildRoleCommandHandler> _logger;

    public RevokeGuildRoleCommandHandler(IGuildMemberRepository guildMemberRepository, ILogger<RevokeGuildRoleCommandHandler> logger)
    {
        _guildMemberRepository = guildMemberRepository;
        _logger = logger;
    }

    // Convenience overload for tests that don't pass a logger
    public RevokeGuildRoleCommandHandler(IGuildMemberRepository guildMemberRepository)
        : this(guildMemberRepository, LoggerFactory.Create(builder => { }).CreateLogger<RevokeGuildRoleCommandHandler>())
    {
    }

    public async Task<Unit> Handle(RevokeGuildRoleCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsAdminOverride)
        {
            var isMaster = await _guildMemberRepository.IsGuildMasterAsync(request.GuildId, request.ActorAuthUserId, cancellationToken);
            if (!isMaster)
            {
                throw new ForbiddenException("Only GuildMaster may revoke roles in this guild.");
            }
        }

        var member = await _guildMemberRepository.GetMemberAsync(request.GuildId, request.MemberAuthUserId, cancellationToken)
            ?? throw new NotFoundException("GuildMember", request.MemberAuthUserId.ToString());

        if (request.RoleToRevoke == GuildRole.GuildMaster)
        {
            throw new UnprocessableEntityException("Cannot revoke GuildMaster via role management. Use transfer leadership endpoint.");
        }

        // Idempotency: if member doesn't have the role, no-op
        if (member.Role != request.RoleToRevoke)
        {
            return Unit.Value; // controller should map to 204
        }

        // Ensure baseline role remains; cannot revoke baseline Member or Recruit (would leave no role)
        if (request.RoleToRevoke == GuildRole.Member || request.RoleToRevoke == GuildRole.Recruit)
        {
            throw new UnprocessableEntityException("Cannot revoke baseline role. To remove a member, use remove member endpoint.");
        }

        var oldRole = member.Role;
        // Revoke role by setting to baseline Member
        member.Role = GuildRole.Member;
        await _guildMemberRepository.UpdateAsync(member, cancellationToken);

        _logger.LogInformation("Guild role revoked: GuildId={GuildId}, MemberAuthUserId={MemberId}, ActorAuthUserId={ActorId}, OldRole={OldRole}, NewRole={NewRole}, AdminOverride={AdminOverride}",
            request.GuildId, request.MemberAuthUserId, request.ActorAuthUserId, oldRole, member.Role, request.IsAdminOverride);

        return Unit.Value;
    }
}