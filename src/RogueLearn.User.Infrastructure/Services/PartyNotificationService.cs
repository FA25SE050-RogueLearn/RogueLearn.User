using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Infrastructure.Services;

/// <summary>
/// Stub implementation for party notification service.
/// TODO: Integrate with actual notification system (email, push notifications, etc.)
/// </summary>
public class PartyNotificationService : IPartyNotificationService
{
    private readonly ILogger<PartyNotificationService> _logger;

    public PartyNotificationService(ILogger<PartyNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task SendInvitationNotificationAsync(PartyInvitation invitation, CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual notification logic (email, push notification, etc.)
        _logger.LogInformation("Invitation notification sent for Party {PartyId} to User {InviteeId}", 
            invitation.PartyId, invitation.InviteeId);
        
        await Task.CompletedTask;
    }

    public async Task SendMaterialUploadNotificationAsync(PartyStashItem stashItem, CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual notification logic (email, push notification, etc.)
        _logger.LogInformation("Material upload notification sent for Party {PartyId}, Item: {Title}", 
            stashItem.PartyId, stashItem.Title);
        
        await Task.CompletedTask;
    }
}