using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;

public class InviteGuildMembersCommandHandler : IRequestHandler<InviteGuildMembersCommand, InviteGuildMembersResponse>
{
    private readonly IGuildInvitationRepository _invitationRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IGuildRepository? _guildRepository;
    private readonly IGuildMemberRepository? _guildMemberRepository;
    private readonly IRoleRepository? _roleRepository;
    private readonly IUserRoleRepository? _userRoleRepository;
    private readonly IGuildNotificationService? _notificationService;

    public InviteGuildMembersCommandHandler(
        IGuildInvitationRepository invitationRepository,
        IUserProfileRepository userProfileRepository,
        IGuildRepository guildRepository,
        IGuildMemberRepository guildMemberRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IGuildNotificationService notificationService)
    {
        _invitationRepository = invitationRepository;
        _userProfileRepository = userProfileRepository;
        _guildRepository = guildRepository;
        _guildMemberRepository = guildMemberRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _notificationService = notificationService;
    }

    public async Task<InviteGuildMembersResponse> Handle(InviteGuildMembersCommand request, CancellationToken cancellationToken)
    {
        var createdIds = new List<Guid>();
        if (_guildRepository != null && _guildMemberRepository != null && _roleRepository != null && _userRoleRepository != null)
        {
            var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
                ?? throw new Application.Exceptions.NotFoundException("Guild", request.GuildId.ToString());

            var master = (await _guildMemberRepository.GetMembersByGuildAsync(request.GuildId, cancellationToken))
                .FirstOrDefault(m => m.Status == MemberStatus.Active && m.Role == GuildRole.GuildMaster);
            var verifiedLecturerRole = await _roleRepository.GetByNameAsync("Verified Lecturer", cancellationToken);
            var isMasterVerifiedLecturer = false;
            if (master != null && verifiedLecturerRole != null)
            {
                var masterRoles = await _userRoleRepository.GetRolesForUserAsync(master.AuthUserId, cancellationToken);
                isMasterVerifiedLecturer = masterRoles.Any(r => r.RoleId == verifiedLecturerRole.Id);
            }

            if (!isMasterVerifiedLecturer)
            {
                var cap = Math.Min(guild.MaxMembers, 50);
                if (guild.CurrentMemberCount >= cap)
                {
                    throw new Application.Exceptions.UnprocessableEntityException("Invitations are disabled until a Verified Lecturer is GuildMaster.");
                }
            }
        }
        var pending = await _invitationRepository.GetPendingInvitationsByGuildAsync(request.GuildId, cancellationToken);

        foreach (var target in request.Targets)
        {
            Guid? inviteeId = target.UserId;

            if (!inviteeId.HasValue && string.IsNullOrWhiteSpace(target.Email))
            {
                throw new Exceptions.BadRequestException("Invite target must include userId or email.");
            }

            if (!inviteeId.HasValue && !string.IsNullOrWhiteSpace(target.Email))
            {
                var profile = await _userProfileRepository.GetByEmailAsync(target.Email, cancellationToken);
                if (profile != null)
                {
                    inviteeId = profile.AuthUserId;
                }
                else
                {
                    throw new Exceptions.BadRequestException($"No user found with email '{target.Email}'.");
                }
            }

            if (!inviteeId.HasValue)
            {
                throw new Exceptions.BadRequestException("Invalid invite target.");
            }

            if (inviteeId.Value == request.InviterAuthUserId)
            {
                throw new Exceptions.BadRequestException("Cannot invite yourself to the guild.");
            }
            if (pending.Any(i => i.InviteeId == inviteeId.Value))
            {
                throw new Exceptions.BadRequestException("An invitation is already pending for this user.");
            }

            if (_guildMemberRepository != null)
            {
                var memberships = await _guildMemberRepository.GetMembershipsByUserAsync(inviteeId.Value, cancellationToken);
                var belongsOtherGuild = memberships.Any(m => m.Status == MemberStatus.Active && m.GuildId != request.GuildId);
                var alreadyMemberHere = memberships.Any(m => m.Status == MemberStatus.Active && m.GuildId == request.GuildId);
                if (belongsOtherGuild || alreadyMemberHere)
                {
                    throw new Exceptions.BadRequestException($"User {inviteeId.Value} is already a member of another guild.");
                }
            }

            var existing = await _invitationRepository.GetByGuildAndInviteeAsync(request.GuildId, inviteeId.Value, cancellationToken);
            if (existing is not null)
            {
                if (existing.Status == InvitationStatus.Pending && existing.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    throw new Exceptions.BadRequestException("An invitation is already pending for this user.");
                }

                existing.InviterId = request.InviterAuthUserId;
                existing.InvitationType = InvitationType.Invite;
                existing.Status = InvitationStatus.Pending;
                existing.Message = request.Message;
                existing.CreatedAt = DateTimeOffset.UtcNow;
                existing.RespondedAt = null;
                existing.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);

                var updated = await _invitationRepository.UpdateAsync(existing, cancellationToken);
                createdIds.Add(updated.Id);
                if (_notificationService != null)
                {
                    await _notificationService.NotifyInvitationCreatedAsync(updated, cancellationToken);
                }
            }
            else
            {
                var invitation = new GuildInvitation
                {
                    GuildId = request.GuildId,
                    InviterId = request.InviterAuthUserId,
                    InviteeId = inviteeId.Value,
                    InvitationType = InvitationType.Invite,
                    Status = InvitationStatus.Pending,
                    Message = request.Message,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
                };

                invitation = await _invitationRepository.AddAsync(invitation, cancellationToken);
                createdIds.Add(invitation.Id);
                if (_notificationService != null)
                {
                    await _notificationService.NotifyInvitationCreatedAsync(invitation, cancellationToken);
                }
            }
        }
        if (createdIds.Count == 0)
        {
            throw new Exceptions.BadRequestException("No valid invite targets.");
        }
        return new InviteGuildMembersResponse(createdIds);
    }
}