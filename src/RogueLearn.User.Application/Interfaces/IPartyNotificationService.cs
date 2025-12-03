using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Interfaces;

/// <summary>
/// Service for sending party-related notifications.
/// </summary>
public interface IPartyNotificationService
{
    /// <summary>
    /// Sends a notification when a member is invited to a party.
    /// </summary>
    /// <param name="invitation">The party invitation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendInvitationNotificationAsync(PartyInvitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when material is uploaded to a party stash.
    /// </summary>
    /// <param name="stashItem">The uploaded stash item details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendInvitationAcceptedNotificationAsync(PartyInvitation invitation, CancellationToken cancellationToken = default);
    Task SendInvitationDeclinedNotificationAsync(PartyInvitation invitation, CancellationToken cancellationToken = default);
    Task SendMemberJoinedNotificationAsync(Guid partyId, Guid userId, IEnumerable<Guid> leaderAuthUserIds, CancellationToken cancellationToken = default);
    Task SendMemberLeftNotificationAsync(Guid partyId, Guid userId, IEnumerable<Guid> leaderAuthUserIds, CancellationToken cancellationToken = default);
    Task SendMemberRemovedNotificationAsync(Guid partyId, Guid memberAuthUserId, CancellationToken cancellationToken = default);
    Task SendLeadershipTransferredNotificationAsync(Guid partyId, Guid newLeaderAuthUserId, CancellationToken cancellationToken = default);
}