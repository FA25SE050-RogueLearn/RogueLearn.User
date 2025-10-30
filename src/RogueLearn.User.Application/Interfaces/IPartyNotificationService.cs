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
    Task SendMaterialUploadNotificationAsync(PartyStashItem stashItem, CancellationToken cancellationToken = default);
}