using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

/// <summary>
/// Stub implementation for party notification service.
/// TODO: Integrate with actual notification system (email, push notifications, etc.)
/// </summary>
public class PartyNotificationService : IPartyNotificationService
{
    private readonly ILogger<PartyNotificationService> _logger;
    private readonly INotificationRepository _notificationRepository;
    private readonly IPartyRepository? _partyRepository;
    private readonly IUserProfileRepository? _userProfileRepository;

    public PartyNotificationService(ILogger<PartyNotificationService> logger, INotificationRepository notificationRepository, IPartyRepository partyRepository, IUserProfileRepository userProfileRepository)
    {
        _logger = logger;
        _notificationRepository = notificationRepository;
        _partyRepository = partyRepository;
        _userProfileRepository = userProfileRepository;
    }

    private async Task<string> GetPartyNameAsync(Guid partyId, CancellationToken cancellationToken)
    {
        if (_partyRepository == null) return string.Empty;
        var p = await _partyRepository.GetByIdAsync(partyId, cancellationToken);
        return p?.Name ?? string.Empty;
    }

    private async Task<string> GetUserNameAsync(Guid authUserId, CancellationToken cancellationToken)
    {
        if (_userProfileRepository == null) return string.Empty;
        var prof = await _userProfileRepository.GetByAuthIdAsync(authUserId, cancellationToken);
        var first = prof?.FirstName?.Trim();
        var last = prof?.LastName?.Trim();
        var both = string.Join(" ", new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(both) ? (prof?.Username ?? string.Empty) : both;
    }

    public async Task SendInvitationNotificationAsync(PartyInvitation invitation, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invitation notification sent for Party {PartyId} to User {InviteeId}", invitation.PartyId, invitation.InviteeId);
        var partyName = await GetPartyNameAsync(invitation.PartyId, cancellationToken);
        var inviterName = await GetUserNameAsync(invitation.InviterId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = invitation.InviteeId,
            Type = NotificationType.Party,
            Title = "Party invitation",
            Message = $"You have been invited to a party {partyName} by {inviterName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["partyId"] = invitation.PartyId,
                ["inviterId"] = invitation.InviterId,
                ["inviterName"] = inviterName,
                ["partyName"] = partyName
            }
        }, cancellationToken);
    }

    public async Task SendInvitationAcceptedNotificationAsync(PartyInvitation invitation, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Party invitation accepted for Party {PartyId} by User {InviteeId}", invitation.PartyId, invitation.InviteeId);
        var partyName = await GetPartyNameAsync(invitation.PartyId, cancellationToken);
        var inviteeName = await GetUserNameAsync(invitation.InviteeId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = invitation.InviterId,
            Type = NotificationType.Party,
            Title = "Party invitation accepted",
            Message = $"Your invitation to join party {partyName} was accepted by {inviteeName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["partyId"] = invitation.PartyId,
                ["inviteeId"] = invitation.InviteeId,
                ["inviteeName"] = inviteeName,
                ["partyName"] = partyName
            }
        }, cancellationToken);
    }

    public async Task SendInvitationDeclinedNotificationAsync(PartyInvitation invitation, CancellationToken cancellationToken = default)
    {
        var partyName = await GetPartyNameAsync(invitation.PartyId, cancellationToken);
        var inviteeName = await GetUserNameAsync(invitation.InviteeId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = invitation.InviterId,
            Type = NotificationType.Party,
            Title = "Party invitation declined",
            Message = $"An invitation to join party {partyName} you sent was declined by {inviteeName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["partyId"] = invitation.PartyId,
                ["inviteeId"] = invitation.InviteeId,
                ["inviteeName"] = inviteeName,
                ["partyName"] = partyName
            }
        }, cancellationToken);
    }

    public async Task SendMemberJoinedNotificationAsync(Guid partyId, Guid userId, IEnumerable<Guid> leaderAuthUserIds, CancellationToken cancellationToken = default)
    {
        var partyName = await GetPartyNameAsync(partyId, cancellationToken);
        var memberName = await GetUserNameAsync(userId, cancellationToken);
        foreach (var leaderId in leaderAuthUserIds)
        {
            await _notificationRepository.AddAsync(new Notification
            {
                AuthUserId = leaderId,
                Type = NotificationType.Party,
                Title = "Member joined party",
                Message = $"A member {memberName} joined your party {partyName}.",
                CreatedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["partyId"] = partyId,
                    ["memberId"] = userId,
                    ["memberName"] = memberName,
                    ["partyName"] = partyName
                }
            }, cancellationToken);
        }
    }

    public async Task SendMemberLeftNotificationAsync(Guid partyId, Guid userId, IEnumerable<Guid> leaderAuthUserIds, CancellationToken cancellationToken = default)
    {
        var partyName = await GetPartyNameAsync(partyId, cancellationToken);
        var memberName = await GetUserNameAsync(userId, cancellationToken);
        foreach (var leaderId in leaderAuthUserIds)
        {
            await _notificationRepository.AddAsync(new Notification
            {
                AuthUserId = leaderId,
                Type = NotificationType.Party,
                Title = "Member left party",
                Message = $"A member {memberName} left your party {partyName}.",
                CreatedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["partyId"] = partyId,
                    ["memberId"] = userId,
                    ["memberName"] = memberName,
                    ["partyName"] = partyName
                }
            }, cancellationToken);
        }
    }

    public async Task SendMemberRemovedNotificationAsync(Guid partyId, Guid memberAuthUserId, CancellationToken cancellationToken = default)
    {
        var partyName = await GetPartyNameAsync(partyId, cancellationToken);
        var memberName = await GetUserNameAsync(memberAuthUserId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = memberAuthUserId,
            Type = NotificationType.Party,
            Title = "Removed from party",
            Message = $"You were removed from the party {partyName} by {memberName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["partyId"] = partyId,
                ["partyName"] = partyName,
                ["memberName"] = memberName
            }
        }, cancellationToken);
    }

    public async Task SendLeadershipTransferredNotificationAsync(Guid partyId, Guid newLeaderAuthUserId, CancellationToken cancellationToken = default)
    {
        var partyName = await GetPartyNameAsync(partyId, cancellationToken);
        var newLeaderName = await GetUserNameAsync(newLeaderAuthUserId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = newLeaderAuthUserId,
            Type = NotificationType.Party,
            Title = "Party leadership transferred",
            Message = $"You are now the party leader of {partyName}.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["partyId"] = partyId,
                ["partyName"] = partyName,
                ["leaderName"] = newLeaderName
            }
        }, cancellationToken);
    }
}