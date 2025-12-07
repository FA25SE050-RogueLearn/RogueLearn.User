using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.LeaveParty;

public class LeavePartyCommandHandler : IRequestHandler<LeavePartyCommand, Unit>
{
    private readonly IPartyMemberRepository _memberRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;

    public LeavePartyCommandHandler(IPartyMemberRepository memberRepository, IPartyRepository partyRepository, IUserRoleRepository userRoleRepository, IRoleRepository roleRepository)
    {
        _memberRepository = memberRepository;
        _partyRepository = partyRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
    }

    public async Task<Unit> Handle(LeavePartyCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetMemberAsync(request.PartyId, request.AuthUserId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("PartyMember", request.AuthUserId.ToString());

        if (member.Role == PartyRole.Leader)
        {
            var activeCount = await _memberRepository.CountActiveMembersAsync(request.PartyId, cancellationToken);
            if (activeCount > 1)
            {
                // Transfer ownership to the next highest role (Member)
                var members = await _memberRepository.GetMembersByPartyAsync(request.PartyId, cancellationToken);
                var eligible = members
                    .Where(m => m.Status == MemberStatus.Active && m.AuthUserId != request.AuthUserId)
                    .ToList();

                var nextOwner = eligible
                    .Where(m => m.Role == PartyRole.Member)
                    .OrderBy(m => m.JoinedAt)
                    .FirstOrDefault();

                if (nextOwner == null)
                {
                    // Fallback: pick any active member (should not happen when activeCount > 1)
                    nextOwner = eligible.OrderBy(m => m.JoinedAt).FirstOrDefault();
                }

                if (nextOwner == null)
                {
                    // No eligible successor found; delete leaving member then delete party
                    await _memberRepository.DeleteAsync(member.Id, cancellationToken);
                    await _partyRepository.DeleteAsync(request.PartyId, cancellationToken);
                    return Unit.Value;
                }

                nextOwner.Role = PartyRole.Leader;
                await _memberRepository.UpdateAsync(nextOwner, cancellationToken);

                if (_userRoleRepository != null && _roleRepository != null)
                {
                    var partyLeaderRole = await _roleRepository.GetByNameAsync("Party Leader", cancellationToken)
                        ?? throw new Exceptions.NotFoundException("Role", "Party Leader");

                    var leavingUserRoles = await _userRoleRepository.GetRolesForUserAsync(member.AuthUserId, cancellationToken);
                    foreach (var ur in leavingUserRoles.Where(r => r.RoleId == partyLeaderRole.Id))
                    {
                        await _userRoleRepository.DeleteAsync(ur.Id, cancellationToken);
                    }

                    var newLeaderRoles = await _userRoleRepository.GetRolesForUserAsync(nextOwner.AuthUserId, cancellationToken);
                    if (!newLeaderRoles.Any(r => r.RoleId == partyLeaderRole.Id))
                    {
                        var userRole = new RogueLearn.User.Domain.Entities.UserRole
                        {
                            Id = Guid.NewGuid(),
                            AuthUserId = nextOwner.AuthUserId,
                            RoleId = partyLeaderRole.Id,
                            AssignedAt = DateTimeOffset.UtcNow,
                            AssignedBy = member.AuthUserId
                        };
                        await _userRoleRepository.AddAsync(userRole, cancellationToken);
                    }
                }
            }
            else
            {
                // Sole member is the leader; delete leaving member then delete the party
                await _memberRepository.DeleteAsync(member.Id, cancellationToken);
                await _partyRepository.DeleteAsync(request.PartyId, cancellationToken);
                if (_userRoleRepository != null && _roleRepository != null)
                {
                    var partyLeaderRole = await _roleRepository.GetByNameAsync("Party Leader", cancellationToken)
                        ?? throw new Exceptions.NotFoundException("Role", "Party Leader");
                    var leavingUserRoles = await _userRoleRepository.GetRolesForUserAsync(member.AuthUserId, cancellationToken);
                    foreach (var ur in leavingUserRoles.Where(r => r.RoleId == partyLeaderRole.Id))
                    {
                        await _userRoleRepository.DeleteAsync(ur.Id, cancellationToken);
                    }
                }
                return Unit.Value;
            }
        }

        // Remove the leaving member from the party
        await _memberRepository.DeleteAsync(member.Id, cancellationToken);
        return Unit.Value;
    }
}