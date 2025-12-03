using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

public class GuildNotificationService : IGuildNotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IGuildRepository? _guildRepository;
    private readonly IUserProfileRepository? _userProfileRepository;

    public GuildNotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
        _guildRepository = null;
        _userProfileRepository = null;
    }

    public GuildNotificationService(INotificationRepository notificationRepository, IGuildRepository guildRepository, IUserProfileRepository userProfileRepository)
    {
        _notificationRepository = notificationRepository;
        _guildRepository = guildRepository;
        _userProfileRepository = userProfileRepository;
    }

    private async Task<string> GetGuildNameAsync(Guid guildId, CancellationToken cancellationToken)
    {
        if (_guildRepository == null) return string.Empty;
        var g = await _guildRepository.GetByIdAsync(guildId, cancellationToken);
        return g?.Name ?? string.Empty;
    }

    private async Task<string> GetUserNameAsync(Guid authUserId, CancellationToken cancellationToken)
    {
        if (_userProfileRepository == null) return string.Empty;
        var p = await _userProfileRepository.GetByAuthIdAsync(authUserId, cancellationToken);
        var first = p?.FirstName?.Trim();
        var last = p?.LastName?.Trim();
        var both = string.Join(" ", new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(both) ? (p?.Username ?? string.Empty) : both;
    }

    public async Task NotifyInvitationAcceptedAsync(GuildInvitation invitation, CancellationToken cancellationToken = default)
    {
        if (invitation.InviterId.HasValue)
        {
            var guildName = await GetGuildNameAsync(invitation.GuildId, cancellationToken);
            var inviteeName = await GetUserNameAsync(invitation.InviteeId, cancellationToken);
            await _notificationRepository.AddAsync(new Notification
            {
                AuthUserId = invitation.InviterId.Value,
                Type = NotificationType.Guild,
                Title = "Guild invitation accepted",
                Message = $"Your guild invitation was accepted by {inviteeName}.",
                CreatedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["guildId"] = invitation.GuildId,
                    ["inviteeId"] = invitation.InviteeId,
                    ["inviteeName"] = inviteeName,
                    ["guildName"] = guildName
                }
            }, cancellationToken);
        }
    }

    public async Task NotifyJoinRequestSubmittedAsync(Guid recipientAuthUserId, GuildJoinRequest request, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(request.GuildId, cancellationToken);
        var requesterName = await GetUserNameAsync(request.RequesterId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = recipientAuthUserId,
            Type = NotificationType.Guild,
            Title = "New guild join request",
            Message = $"A user {requesterName} has requested to join your guild.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = request.GuildId,
                ["requesterId"] = request.RequesterId,
                ["requesterName"] = requesterName,
                ["guildName"] = guildName
            }
        }, cancellationToken);
    }

    public async Task NotifyJoinRequestApprovedAsync(GuildJoinRequest request, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(request.GuildId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = request.RequesterId,
            Type = NotificationType.Guild,
            Title = "Guild join request approved",
            Message = $"Your request to join the guild {guildName} was approved.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = request.GuildId,
                ["guildName"] = guildName
            }
        }, cancellationToken);
    }

    public async Task NotifyInvitationDeclinedAsync(GuildInvitation invitation, Guid guildMasterAuthUserId, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(invitation.GuildId, cancellationToken);
        var inviteeName = await GetUserNameAsync(invitation.InviteeId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = guildMasterAuthUserId,
            Type = NotificationType.Guild,
            Title = "Guild invitation declined",
            Message = $"An invitation you sent to {inviteeName} was declined.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = invitation.GuildId,
                ["inviteeId"] = invitation.InviteeId,
                ["inviteeName"] = inviteeName,
                ["guildName"] = guildName
            }
        }, cancellationToken);
    }

    public async Task NotifyJoinRequestDeclinedAsync(GuildJoinRequest request, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(request.GuildId, cancellationToken);
        var requesterName = await GetUserNameAsync(request.RequesterId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = request.RequesterId,
            Type = NotificationType.Guild,
            Title = "Guild join request declined",
            Message = $"Your request to join the guild {guildName} was declined.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = request.GuildId,
                ["guildName"] = guildName,
                ["requesterName"] = requesterName
            }
        }, cancellationToken);
    }

    public async Task NotifyInvitationCreatedAsync(GuildInvitation invitation, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(invitation.GuildId, cancellationToken);
        var inviterName = invitation.InviterId.HasValue ? await GetUserNameAsync(invitation.InviterId.Value, cancellationToken) : string.Empty;
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = invitation.InviteeId,
            Type = NotificationType.Guild,
            Title = "Guild invitation",
            Message = $"You have been invited to join the guild {guildName} by {inviterName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = invitation.GuildId,
                ["inviterId"] = invitation.InviterId!,
                ["inviterName"] = inviterName,
                ["guildName"] = guildName
            }
        }, cancellationToken);
    }

    public async Task NotifyMemberLeftAsync(Guid guildId, Guid memberAuthUserId, Guid guildMasterAuthUserId, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(guildId, cancellationToken);
        var memberName = await GetUserNameAsync(memberAuthUserId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = guildMasterAuthUserId,
            Type = NotificationType.Guild,
            Title = "Member left guild",
            Message = $"A member {memberName} left your guild.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = guildId,
                ["memberId"] = memberAuthUserId,
                ["memberName"] = memberName,
                ["guildName"] = guildName
            }
        }, cancellationToken);
    }

    public async Task NotifyMemberRemovedAsync(Guid guildId, Guid memberAuthUserId, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(guildId, cancellationToken);
        var memberName = await GetUserNameAsync(memberAuthUserId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = memberAuthUserId,
            Type = NotificationType.Guild,
            Title = "Removed from guild",
            Message = $"You were removed from the guild {guildName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = guildId,
                ["guildName"] = guildName,
                ["memberName"] = memberName
            }
        }, cancellationToken);
    }

    public async Task NotifyRoleAssignedAsync(Guid guildId, Guid memberAuthUserId, GuildRole role, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(guildId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = memberAuthUserId,
            Type = NotificationType.Guild,
            Title = "Guild role assigned",
            Message = $"A new role {role} was assigned to you in the guild {guildName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = guildId,
                ["role"] = role.ToString(),
                ["guildName"] = guildName
            }
        }, cancellationToken);
    }

    public async Task NotifyRoleRevokedAsync(Guid guildId, Guid memberAuthUserId, GuildRole role, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(guildId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = memberAuthUserId,
            Type = NotificationType.Guild,
            Title = "Guild role revoked",
            Message = $"A role {role} was revoked from you in the guild {guildName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = guildId,
                ["role"] = role.ToString(),
                ["guildName"] = guildName
            }
        }, cancellationToken);
    }

    public async Task NotifyLeadershipTransferredAsync(Guid guildId, Guid newLeaderAuthUserId, CancellationToken cancellationToken = default)
    {
        var guildName = await GetGuildNameAsync(guildId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = newLeaderAuthUserId,
            Type = NotificationType.Guild,
            Title = "Guild leadership transferred",
            Message = $"You are now the guild master of {guildName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["guildId"] = guildId,
                ["guildName"] = guildName
            }
        }, cancellationToken);
    }
}