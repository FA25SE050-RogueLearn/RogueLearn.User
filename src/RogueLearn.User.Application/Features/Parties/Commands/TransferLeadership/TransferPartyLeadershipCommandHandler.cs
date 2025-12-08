using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.TransferLeadership;

public class TransferPartyLeadershipCommandHandler : IRequestHandler<TransferPartyLeadershipCommand, Unit>
{
    private readonly IPartyMemberRepository _memberRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPartyNotificationService? _notificationService;

    public TransferPartyLeadershipCommandHandler(IPartyMemberRepository memberRepository, IUserRoleRepository userRoleRepository, IRoleRepository roleRepository, IPartyNotificationService notificationService)
    {
        _memberRepository = memberRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(TransferPartyLeadershipCommand request, CancellationToken cancellationToken)
    {
        var members = await _memberRepository.GetMembersByPartyAsync(request.PartyId, cancellationToken);

        var newLeader = members.FirstOrDefault(m => m.AuthUserId == request.ToUserId && m.Status == MemberStatus.Active)
                        ?? throw new Application.Exceptions.NotFoundException("PartyMember", request.ToUserId.ToString());

        var leaders = members.Where(m => m.Role == PartyRole.Leader).ToList();
        if (!leaders.Any())
        {
            throw new Application.Exceptions.NotFoundException("PartyLeader", request.PartyId.ToString());
        }

        foreach (var leader in leaders)
        {
            if (leader.AuthUserId != newLeader.AuthUserId)
            {
                leader.Role = PartyRole.Member;
                await _memberRepository.UpdateAsync(leader, cancellationToken);
            }
        }

        newLeader.Role = PartyRole.Leader;
        await _memberRepository.UpdateAsync(newLeader, cancellationToken);

        if (_userRoleRepository != null && _roleRepository != null)
        {
            var partyLeaderRole = await _roleRepository.GetByNameAsync("Party Leader", cancellationToken)
                ?? throw new Application.Exceptions.NotFoundException("Role", "Party Leader");

            foreach (var leader in leaders)
            {
                if (leader.AuthUserId == newLeader.AuthUserId) continue;
                var userRoles = await _userRoleRepository.GetRolesForUserAsync(leader.AuthUserId, cancellationToken);
                foreach (var ur in userRoles.Where(r => r.RoleId == partyLeaderRole.Id))
                {
                    await _userRoleRepository.DeleteAsync(ur.Id, cancellationToken);
                }
            }

            var newUserRoles = await _userRoleRepository.GetRolesForUserAsync(newLeader.AuthUserId, cancellationToken);
            if (!newUserRoles.Any(ur => ur.RoleId == partyLeaderRole.Id))
            {
                var userRole = new RogueLearn.User.Domain.Entities.UserRole
                {
                    Id = Guid.NewGuid(),
                    AuthUserId = newLeader.AuthUserId,
                    RoleId = partyLeaderRole.Id,
                    AssignedAt = DateTimeOffset.UtcNow,
                    AssignedBy = newLeader.AuthUserId
                };
                await _userRoleRepository.AddAsync(userRole, cancellationToken);
            }
        }

        if (_notificationService != null)
        {
            await _notificationService.SendLeadershipTransferredNotificationAsync(request.PartyId, newLeader.AuthUserId, cancellationToken);
        }

        return Unit.Value;
    }
}