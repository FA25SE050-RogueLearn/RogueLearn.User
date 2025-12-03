using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.TransferLeadership;

public class TransferGuildLeadershipCommandHandler : IRequestHandler<TransferGuildLeadershipCommand, Unit>
{
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IGuildNotificationService? _notificationService;

    public TransferGuildLeadershipCommandHandler(IGuildMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
        _userRoleRepository = null!;
        _roleRepository = null!;
        _notificationService = null;
    }

    public TransferGuildLeadershipCommandHandler(IGuildMemberRepository memberRepository, IUserRoleRepository userRoleRepository, IRoleRepository roleRepository)
    {
        _memberRepository = memberRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _notificationService = null;
    }

    public TransferGuildLeadershipCommandHandler(IGuildMemberRepository memberRepository, IUserRoleRepository userRoleRepository, IRoleRepository roleRepository, IGuildNotificationService notificationService)
    {
        _memberRepository = memberRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(TransferGuildLeadershipCommand request, CancellationToken cancellationToken)
    {
        var members = await _memberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);

        var newMaster = members.FirstOrDefault(m => m.AuthUserId == request.ToUserId && m.Status == MemberStatus.Active)
                        ?? throw new Application.Exceptions.NotFoundException("GuildMember", request.ToUserId.ToString());

        var masters = members.Where(m => m.Role == GuildRole.GuildMaster).ToList();
        if (!masters.Any())
        {
            throw new Application.Exceptions.NotFoundException("GuildMaster", request.GuildId.ToString());
        }

        foreach (var master in masters)
        {
            if (master.AuthUserId != newMaster.AuthUserId)
            {
                master.Role = GuildRole.Member;
                await _memberRepository.UpdateAsync(master, cancellationToken);
            }
        }

        newMaster.Role = GuildRole.GuildMaster;
        await _memberRepository.UpdateAsync(newMaster, cancellationToken);

        if (_userRoleRepository != null && _roleRepository != null)
        {
            var guildMasterRole = await _roleRepository.GetByNameAsync("Guild Master", cancellationToken)
                ?? throw new Application.Exceptions.NotFoundException("Role", "Guild Master");

            foreach (var master in masters)
            {
                if (master.AuthUserId == newMaster.AuthUserId) continue;

                var userRoles = await _userRoleRepository.GetRolesForUserAsync(master.AuthUserId, cancellationToken);
                foreach (var ur in userRoles.Where(r => r.RoleId == guildMasterRole.Id))
                {
                    await _userRoleRepository.DeleteAsync(ur.Id, cancellationToken);
                }
            }

            var newUserRoles = await _userRoleRepository.GetRolesForUserAsync(newMaster.AuthUserId, cancellationToken);
            if (!newUserRoles.Any(ur => ur.RoleId == guildMasterRole.Id))
            {
                var userRole = new RogueLearn.User.Domain.Entities.UserRole
                {
                    Id = Guid.NewGuid(),
                    AuthUserId = newMaster.AuthUserId,
                    RoleId = guildMasterRole.Id,
                    AssignedAt = DateTimeOffset.UtcNow,
                    AssignedBy = newMaster.AuthUserId
                };
                await _userRoleRepository.AddAsync(userRole, cancellationToken);
            }
        }

        if (_notificationService != null)
        {
            await _notificationService.NotifyLeadershipTransferredAsync(request.GuildId, newMaster.AuthUserId, cancellationToken);
        }

        return Unit.Value;
    }
}