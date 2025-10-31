using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.ManageRoles;

public class RevokePartyRoleCommandHandler : IRequestHandler<RevokePartyRoleCommand, Unit>
{
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly ILogger<RevokePartyRoleCommandHandler> _logger;

    public RevokePartyRoleCommandHandler(IPartyMemberRepository partyMemberRepository, ILogger<RevokePartyRoleCommandHandler> logger)
    {
        _partyMemberRepository = partyMemberRepository;
        _logger = logger;
    }

    // Convenience overload for tests that don't pass a logger
    public RevokePartyRoleCommandHandler(IPartyMemberRepository partyMemberRepository)
        : this(partyMemberRepository, LoggerFactory.Create(builder => { }).CreateLogger<RevokePartyRoleCommandHandler>())
    {
    }

    public async Task<Unit> Handle(RevokePartyRoleCommand request, CancellationToken cancellationToken)
    {
        // Authorization: Party Leader of the party OR Platform Admin (handled at controller level)
        if (!request.IsAdminOverride)
        {
            var isLeader = await _partyMemberRepository.IsLeaderAsync(request.PartyId, request.ActorAuthUserId, cancellationToken);
            if (!isLeader)
            {
                throw new ForbiddenException("Only Party Leader may revoke roles in this party.");
            }
        }

        var member = await _partyMemberRepository.GetMemberAsync(request.PartyId, request.MemberAuthUserId, cancellationToken)
            ?? throw new NotFoundException("PartyMember", request.MemberAuthUserId.ToString());

        // Safeguards: cannot revoke Leader via role management; use transfer leadership endpoint
        if (request.RoleToRevoke == PartyRole.Leader)
        {
            throw new UnprocessableEntityException("Cannot revoke Leader via role management. Use transfer leadership endpoint.");
        }

        // Idempotency: revoking a role the member does not have is a no-op
        if (member.Role != request.RoleToRevoke)
        {
            return Unit.Value; // map to 204 No Content
        }

        // Ensure baseline role remains: cannot revoke Member (would leave no role)
        if (request.RoleToRevoke == PartyRole.Member)
        {
            throw new UnprocessableEntityException("Cannot revoke baseline Member role. To remove a member, use the remove member endpoint.");
        }

        var oldRole = member.Role;
        // Revoke role by setting to baseline Member
        member.Role = PartyRole.Member;
        await _partyMemberRepository.UpdateAsync(member, cancellationToken);

        _logger.LogInformation("Party role revoked: PartyId={PartyId}, MemberAuthUserId={MemberId}, ActorAuthUserId={ActorId}, OldRole={OldRole}, NewRole={NewRole}, AdminOverride={AdminOverride}",
            request.PartyId, request.MemberAuthUserId, request.ActorAuthUserId, oldRole, member.Role, request.IsAdminOverride);

        return Unit.Value;
    }
}