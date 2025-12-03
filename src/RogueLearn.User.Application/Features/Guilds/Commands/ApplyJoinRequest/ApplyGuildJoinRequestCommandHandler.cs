using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ApplyJoinRequest;

public class ApplyGuildJoinRequestCommandHandler : IRequestHandler<ApplyGuildJoinRequestCommand, Unit>
{
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildJoinRequestRepository _joinRequestRepository;
    private readonly IGuildNotificationService? _notificationService;
    private readonly IGuildInvitationRepository? _invitationRepository;
    private readonly IRoleRepository? _roleRepository;
    private readonly IUserRoleRepository? _userRoleRepository;

    public ApplyGuildJoinRequestCommandHandler(
        IGuildRepository guildRepository,
        IGuildMemberRepository memberRepository,
        IGuildJoinRequestRepository joinRequestRepository,
        IGuildNotificationService notificationService,
        IGuildInvitationRepository invitationRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository)
    {
        _guildRepository = guildRepository;
        _memberRepository = memberRepository;
        _joinRequestRepository = joinRequestRepository;
        _notificationService = notificationService;
        _invitationRepository = invitationRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<Unit> Handle(ApplyGuildJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("Guild", request.GuildId.ToString());

        // Already a member of this guild?
        var existingMember = await _memberRepository.GetMemberAsync(request.GuildId, request.AuthUserId, cancellationToken);
        if (existingMember is not null && existingMember.Status == MemberStatus.Active)
        {
            throw new Exceptions.BadRequestException("You are already a member of this guild.");
        }

        // Enforce single-guild membership policy
        var memberships = await _memberRepository.GetMembershipsByUserAsync(request.AuthUserId, cancellationToken);
        if (memberships.Any(m => m.Status == MemberStatus.Active && m.GuildId != request.GuildId))
        {
            throw new Exceptions.BadRequestException("You already belong to a different guild.");
        }

        var activeCount = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
        var cap = guild.MaxMembers;
        if (_roleRepository != null && _userRoleRepository != null)
        {
            var members = await _memberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);
            var master = members.FirstOrDefault(m => m.Status == MemberStatus.Active && m.Role == GuildRole.GuildMaster);
            var verifiedLecturerRole = await _roleRepository.GetByNameAsync("Verified Lecturer", cancellationToken);
            var isMasterVerifiedLecturer = false;
            if (master != null && verifiedLecturerRole != null)
            {
                var masterRoles = await _userRoleRepository.GetRolesForUserAsync(master.AuthUserId, cancellationToken);
                isMasterVerifiedLecturer = masterRoles.Any(r => r.RoleId == verifiedLecturerRole.Id);
            }
            if (!isMasterVerifiedLecturer)
            {
                cap = Math.Min(guild.MaxMembers, 50);
            }
        }
        if (activeCount >= cap)
        {
            throw new Exceptions.UnprocessableEntityException("Join requests are disabled until a Verified Lecturer is GuildMaster.");
        }

        // Check for existing pending request
        var myRequests = await _joinRequestRepository.GetRequestsByRequesterAsync(request.AuthUserId, cancellationToken);
        var existingForGuild = myRequests.FirstOrDefault(r => r.GuildId == request.GuildId);
        if (existingForGuild is not null && existingForGuild.Status == GuildJoinRequestStatus.Pending)
        {
            throw new Exceptions.BadRequestException("You already have a pending join request for this guild.");
        }

        if (!guild.RequiresApproval && guild.IsPublic)
        {
            if (existingMember is null)
            {
                var newMember = new GuildMember
                {
                    GuildId = request.GuildId,
                    AuthUserId = request.AuthUserId,
                    Role = GuildRole.Member,
                    Status = MemberStatus.Active,
                    JoinedAt = DateTimeOffset.UtcNow
                };
                await _memberRepository.AddAsync(newMember, cancellationToken);

                var newActiveCount = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
                guild.CurrentMemberCount = newActiveCount;
                guild.UpdatedAt = DateTimeOffset.UtcNow;
                await _guildRepository.UpdateAsync(guild, cancellationToken);

                var members = await _memberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);
                var activeOrdered = members
                    .Where(m => m.Status == MemberStatus.Active)
                    .OrderByDescending(m => m.ContributionPoints)
                    .ThenBy(m => m.JoinedAt)
                    .ToList();

                for (int i = 0; i < activeOrdered.Count; i++)
                {
                    activeOrdered[i].RankWithinGuild = i + 1;
                }

                var nonActive = members.Where(m => m.Status != MemberStatus.Active).ToList();
                foreach (var m in nonActive)
                {
                    m.RankWithinGuild = null;
                }

                await _memberRepository.UpdateRangeAsync(activeOrdered.Concat(nonActive), cancellationToken);
            }

            if (existingForGuild is null)
            {
                var acceptedRecord = new GuildJoinRequest
                {
                    GuildId = request.GuildId,
                    RequesterId = request.AuthUserId,
                    Message = request.Message,
                    Status = GuildJoinRequestStatus.Accepted,
                    CreatedAt = DateTimeOffset.UtcNow,
                    RespondedAt = DateTimeOffset.UtcNow
                };
                await _joinRequestRepository.AddAsync(acceptedRecord, cancellationToken);
                if (_notificationService != null)
                {
                    await _notificationService.NotifyJoinRequestApprovedAsync(acceptedRecord, cancellationToken);
                }
                if (_invitationRepository != null)
                {
                    var otherInvites = await _invitationRepository.FindAsync(i => i.InviteeId == request.AuthUserId, cancellationToken);
                    var toDecline = otherInvites.Where(i => i.Status == InvitationStatus.Pending).ToList();
                    if (toDecline.Any())
                    {
                        foreach (var inv in toDecline)
                        {
                            inv.Status = InvitationStatus.Declined;
                            inv.RespondedAt = DateTimeOffset.UtcNow;
                        }
                        await _invitationRepository.UpdateRangeAsync(toDecline, cancellationToken);
                    }
                }
            }
            else
            {
                existingForGuild.Status = GuildJoinRequestStatus.Accepted;
                existingForGuild.Message = request.Message;
                existingForGuild.RespondedAt = DateTimeOffset.UtcNow;
                await _joinRequestRepository.UpdateAsync(existingForGuild, cancellationToken);
                if (_notificationService != null)
                {
                    await _notificationService.NotifyJoinRequestApprovedAsync(existingForGuild, cancellationToken);
                }
                if (_invitationRepository != null)
                {
                    var otherInvites = await _invitationRepository.FindAsync(i => i.InviteeId == request.AuthUserId, cancellationToken);
                    var toDecline = otherInvites.Where(i => i.Status == InvitationStatus.Pending).ToList();
                    if (toDecline.Any())
                    {
                        foreach (var inv in toDecline)
                        {
                            inv.Status = InvitationStatus.Declined;
                            inv.RespondedAt = DateTimeOffset.UtcNow;
                        }
                        await _invitationRepository.UpdateRangeAsync(toDecline, cancellationToken);
                    }
                }
            }

            var otherRequests = await _joinRequestRepository.GetRequestsByRequesterAsync(request.AuthUserId, cancellationToken);
            var toRemove = otherRequests.Where(r => r.GuildId != request.GuildId && r.Status == GuildJoinRequestStatus.Pending).Select(r => r.Id).ToList();
            if (toRemove.Any())
            {
                await _joinRequestRepository.DeleteRangeAsync(toRemove, cancellationToken);
            }
        }
        else
        {
            if (existingForGuild is null)
            {
                var reqEntity = new GuildJoinRequest
                {
                    GuildId = request.GuildId,
                    RequesterId = request.AuthUserId,
                    Message = request.Message,
                    Status = GuildJoinRequestStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(14)
                };
                await _joinRequestRepository.AddAsync(reqEntity, cancellationToken);
                if (_notificationService != null)
                {
                    var membersForNotify = await _memberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);
                    var masterForNotify = membersForNotify.FirstOrDefault(m => m.Status == MemberStatus.Active && m.Role == GuildRole.GuildMaster);
                    if (masterForNotify != null)
                    {
                        await _notificationService.NotifyJoinRequestSubmittedAsync(masterForNotify.AuthUserId, reqEntity, cancellationToken);
                    }
                }
            }
            else
            {
                existingForGuild.Status = GuildJoinRequestStatus.Pending;
                existingForGuild.Message = request.Message;
                existingForGuild.CreatedAt = DateTimeOffset.UtcNow;
                existingForGuild.RespondedAt = null;
                existingForGuild.ExpiresAt = DateTimeOffset.UtcNow.AddDays(14);
                await _joinRequestRepository.UpdateAsync(existingForGuild, cancellationToken);
                if (_notificationService != null)
                {
                    var membersForNotify = await _memberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken);
                    var masterForNotify = membersForNotify.FirstOrDefault(m => m.Status == MemberStatus.Active && m.Role == GuildRole.GuildMaster);
                    if (masterForNotify != null)
                    {
                        await _notificationService.NotifyJoinRequestSubmittedAsync(masterForNotify.AuthUserId, existingForGuild, cancellationToken);
                    }
                }
            }
        }

        return Unit.Value;
    }
}