namespace RogueLearn.User.Application.Features.Parties.DTOs;

using RogueLearn.User.Application.Features.Parties.Commands.InviteMember;
using RogueLearn.User.Domain.Enums;

public record PartyDto(Guid Id, string Name, string Description, PartyType PartyType, int MaxMembers, bool IsPublic, Guid CreatedBy, DateTimeOffset CreatedAt);
// Extended to include basic user profile information for each party member
public record PartyMemberDto(
    Guid Id,
    Guid PartyId,
    Guid AuthUserId,
    PartyRole Role,
    MemberStatus Status,
    DateTimeOffset JoinedAt,
    string? Username,
    string? Email,
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl,
    int Level,
    int ExperiencePoints,
    string? Bio
);
public record PartyInvitationDto(
    Guid Id,
    Guid PartyId,
    Guid InviterId,
    Guid InviteeId,
    InvitationStatus Status,
    string? Message,
    string? JoinLink,
    Guid? GameSessionId,
    DateTimeOffset InvitedAt,
    DateTimeOffset? RespondedAt,
    DateTimeOffset ExpiresAt,
    string PartyName,
    string InviteeName
);
public record PartyStashItemDto(Guid Id, Guid PartyId, Guid OriginalNoteId, Guid SharedByUserId, string Title, object? Content, IReadOnlyList<string>? Tags, DateTimeOffset SharedAt, DateTimeOffset UpdatedAt);
public record InviteMemberRequest(IReadOnlyList<InviteTarget> Targets, string? Message, DateTimeOffset? ExpiresAt, string? JoinLink = null, Guid? GameSessionId = null);
public record AddPartyResourceRequest(Guid OriginalNoteId, string Title, object Content, IReadOnlyList<string> Tags);
public record UpdatePartyResourceRequest(string Title, object Content, IReadOnlyList<string> Tags);
