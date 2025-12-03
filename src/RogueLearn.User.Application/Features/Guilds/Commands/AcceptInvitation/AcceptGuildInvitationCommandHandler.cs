using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.AcceptInvitation;

public class AcceptGuildInvitationCommandHandler : IRequestHandler<AcceptGuildInvitationCommand, Unit>
{
    private readonly IGuildInvitationRepository _invitationRepository;
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildNotificationService? _notificationService;
    private readonly IGuildJoinRequestRepository? _joinRequestRepository;

    public AcceptGuildInvitationCommandHandler(IGuildInvitationRepository invitationRepository, IGuildMemberRepository memberRepository, IGuildRepository guildRepository, IGuildNotificationService notificationService, IGuildJoinRequestRepository joinRequestRepository)
    {
        _invitationRepository = invitationRepository;
        _memberRepository = memberRepository;
        _guildRepository = guildRepository;
        _notificationService = notificationService;
        _joinRequestRepository = joinRequestRepository;
    }

    public async Task<Unit> Handle(AcceptGuildInvitationCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("Guild", request.GuildId.ToString());

        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("GuildInvitation", request.InvitationId.ToString());

        if (invitation.GuildId != request.GuildId)
        {
            throw new Exceptions.BadRequestException("Invitation does not belong to target guild.");
        }

        if (invitation.Status != InvitationStatus.Pending || invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new Exceptions.BadRequestException("Invitation is not valid.");
        }

        if (invitation.InviteeId != request.AuthUserId)
        {
            throw new Exceptions.ForbiddenException("Invitation not intended for this user.");
        }

        // Enforce one-guild-per-user constraints before adding membership
        // 1) If the user is already an active member of any other guild, they cannot join another guild
        var memberships = await _memberRepository.GetMembershipsByUserAsync(request.AuthUserId, cancellationToken);
        if (memberships.Any(m => m.Status == MemberStatus.Active && m.GuildId != request.GuildId))
        {
            throw new Exceptions.BadRequestException("User already belongs to a guild and cannot join another guild.");
        }

        // 2) If the user has already created a guild, they cannot participate in any other guild
        var createdGuilds = await _guildRepository.GetGuildsByCreatorAsync(request.AuthUserId, cancellationToken);
        if (createdGuilds.Any(g => g.Id != request.GuildId))
        {
            throw new Exceptions.BadRequestException("User is the creator of another guild and cannot join a different guild.");
        }

        // Capacity check
        var count = await _memberRepository.CountActiveMembersAsync(request.GuildId, cancellationToken);
        if (count >= guild.MaxMembers)
        {
            throw new Exceptions.BadRequestException("Guild is at maximum capacity.");
        }

        // Add membership if not exists
        var existing = await _memberRepository.GetMemberAsync(request.GuildId, request.AuthUserId, cancellationToken);
        if (existing is null)
        {
            var member = new GuildMember
            {
                GuildId = request.GuildId,
                AuthUserId = request.AuthUserId,
                Role = GuildRole.Member,
                Status = MemberStatus.Active,
                JoinedAt = DateTimeOffset.UtcNow
            };
            await _memberRepository.AddAsync(member, cancellationToken);

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

        invitation.Status = InvitationStatus.Accepted;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        if (_joinRequestRepository != null)
        {
            var myRequests = await _joinRequestRepository.GetRequestsByRequesterAsync(request.AuthUserId, cancellationToken);
            var toRemove = myRequests.Where(r => r.Status == GuildJoinRequestStatus.Pending).Select(r => r.Id).ToList();
            if (toRemove.Any())
            {
                await _joinRequestRepository.DeleteRangeAsync(toRemove, cancellationToken);
            }
        }

        if (_notificationService != null)
        {
            await _notificationService.NotifyInvitationAcceptedAsync(invitation, cancellationToken);
        }

        var otherForInvitee = await _invitationRepository.FindAsync(
            i => i.InviteeId == request.AuthUserId,
            cancellationToken);
        var toDecline = otherForInvitee
            .Where(i => i.Status == InvitationStatus.Pending && i.GuildId != request.GuildId)
            .ToList();
        if (toDecline.Any())
        {
            foreach (var inv in toDecline)
            {
                inv.Status = InvitationStatus.Declined;
                inv.RespondedAt = DateTimeOffset.UtcNow;
            }
            await _invitationRepository.UpdateRangeAsync(toDecline, cancellationToken);
        }

        return Unit.Value;
    }
}