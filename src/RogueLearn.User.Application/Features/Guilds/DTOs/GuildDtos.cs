namespace RogueLearn.User.Application.Features.Guilds.DTOs;

using RogueLearn.User.Domain.Enums;

public record GuildDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsPublic { get; init; }
    public int MaxMembers { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int MemberCount { get; init; }
}

public record GuildMemberDto
{
    public Guid MemberId { get; init; }
    public Guid GuildId { get; init; }
    public Guid AuthUserId { get; init; }
    public GuildRole Role { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? LeftAt { get; init; }
    public MemberStatus Status { get; init; }
}

public record GuildInvitationDto
{
    // Legacy fields
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public Guid InviteeId { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }

    // Expanded fields for queries
    public Guid InvitationId { get; init; }
    public Guid InviterAuthUserId { get; init; }
    public Guid? TargetUserId { get; init; }
    public string? TargetEmail { get; init; }
    public InvitationStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }
}

public record GuildDashboardDto
{
    public Guid GuildId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int ActiveMemberCount { get; init; }
    public int PendingInvitationCount { get; init; }
    public int AcceptedInvitationCount { get; init; }
    public int MaxMembers { get; init; }
}