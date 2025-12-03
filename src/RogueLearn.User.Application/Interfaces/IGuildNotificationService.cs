namespace RogueLearn.User.Application.Interfaces;

public interface IGuildNotificationService
{
    Task NotifyInvitationAcceptedAsync(RogueLearn.User.Domain.Entities.GuildInvitation invitation, CancellationToken cancellationToken = default);
    Task NotifyJoinRequestSubmittedAsync(Guid recipientAuthUserId, RogueLearn.User.Domain.Entities.GuildJoinRequest request, CancellationToken cancellationToken = default);
    Task NotifyJoinRequestApprovedAsync(RogueLearn.User.Domain.Entities.GuildJoinRequest request, CancellationToken cancellationToken = default);
    Task NotifyInvitationDeclinedAsync(RogueLearn.User.Domain.Entities.GuildInvitation invitation, Guid guildMasterAuthUserId, CancellationToken cancellationToken = default);
    Task NotifyJoinRequestDeclinedAsync(RogueLearn.User.Domain.Entities.GuildJoinRequest request, CancellationToken cancellationToken = default);
    Task NotifyInvitationCreatedAsync(RogueLearn.User.Domain.Entities.GuildInvitation invitation, CancellationToken cancellationToken = default);
    Task NotifyMemberLeftAsync(Guid guildId, Guid memberAuthUserId, Guid guildMasterAuthUserId, CancellationToken cancellationToken = default);
    Task NotifyMemberRemovedAsync(Guid guildId, Guid memberAuthUserId, CancellationToken cancellationToken = default);
    Task NotifyRoleAssignedAsync(Guid guildId, Guid memberAuthUserId, RogueLearn.User.Domain.Enums.GuildRole role, CancellationToken cancellationToken = default);
    Task NotifyRoleRevokedAsync(Guid guildId, Guid memberAuthUserId, RogueLearn.User.Domain.Enums.GuildRole role, CancellationToken cancellationToken = default);
    Task NotifyLeadershipTransferredAsync(Guid guildId, Guid newLeaderAuthUserId, CancellationToken cancellationToken = default);
}