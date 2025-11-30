using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.ManageRoles;

public class AssignPartyRoleCommandHandler : IRequestHandler<AssignPartyRoleCommand, Unit>
{
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly ILogger<AssignPartyRoleCommandHandler> _logger;

    public AssignPartyRoleCommandHandler(IPartyMemberRepository partyMemberRepository, ILogger<AssignPartyRoleCommandHandler> logger)
    {
        _partyMemberRepository = partyMemberRepository;
        _logger = logger;
    }

    // Convenience overload for tests that don't pass a logger
    public AssignPartyRoleCommandHandler(IPartyMemberRepository partyMemberRepository)
        : this(partyMemberRepository, LoggerFactory.Create(builder => { }).CreateLogger<AssignPartyRoleCommandHandler>())
    {
    }

    public async Task<Unit> Handle(AssignPartyRoleCommand request, CancellationToken cancellationToken)
    {
        // Authorization: Party Leader of the party OR Platform Admin (handled at controller level)
        if (!request.IsAdminOverride)
        {
            var isLeader = await _partyMemberRepository.IsLeaderAsync(request.PartyId, request.ActorAuthUserId, cancellationToken);
            if (!isLeader)
            {
                // Controller may also allow PlatformAdmin via attribute; here enforce at least leader
                throw new ForbiddenException("Only Party Leader may assign roles in this party.");
            }
        }

        // Validate target member exists and belongs to party
        var member = await _partyMemberRepository.GetMemberAsync(request.PartyId, request.MemberAuthUserId, cancellationToken)
            ?? throw new NotFoundException("PartyMember", request.MemberAuthUserId.ToString());

        // Safeguards: cannot assign Leader via role management; use transfer leadership endpoint
        if (request.RoleToAssign == PartyRole.Leader)
        {
            throw new UnprocessableEntityException("Cannot assign Leader via role management. Use transfer leadership endpoint.");
        }

        // Safeguards: cannot change an existing Leader via role management
        if (member.Role == PartyRole.Leader)
        {
            throw new UnprocessableEntityException("Cannot modify Leader role via role management. Use transfer leadership endpoint.");
        }

        // Idempotency: if role already equals requested role, return 204 via no-op
        if (member.Role == request.RoleToAssign)
        {
            return Unit.Value; // Caller should map to 204 No Content
        }

        var oldRole = member.Role;
        // Assign role (set the single role)
        member.Role = request.RoleToAssign;
        await _partyMemberRepository.UpdateAsync(member, cancellationToken);

        _logger.LogInformation("Party role changed: PartyId={PartyId}, MemberAuthUserId={MemberId}, ActorAuthUserId={ActorId}, OldRole={OldRole}, NewRole={NewRole}, AdminOverride={AdminOverride}",
            request.PartyId, request.MemberAuthUserId, request.ActorAuthUserId, oldRole, request.RoleToAssign, request.IsAdminOverride);

        return Unit.Value;
    }
}