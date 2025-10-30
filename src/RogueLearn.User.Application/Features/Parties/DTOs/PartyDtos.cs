namespace RogueLearn.User.Application.Features.Parties.DTOs;

using RogueLearn.User.Domain.Enums;

public record PartyDto(Guid Id, string Name, string Description, PartyType PartyType, int MaxMembers, bool IsPublic, Guid CreatedBy, DateTimeOffset CreatedAt);
public record PartyMemberDto(Guid Id, Guid PartyId, Guid AuthUserId, PartyRole Role, MemberStatus Status, DateTimeOffset JoinedAt);
public record PartyInvitationDto(Guid Id, Guid PartyId, Guid InviterId, Guid InviteeId, InvitationStatus Status, string? Message, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt);
public record PartyStashItemDto(Guid Id, Guid PartyId, Guid OriginalNoteId, Guid SharedByUserId, string Title, IReadOnlyDictionary<string, object> Content, IReadOnlyList<string>? Tags, DateTimeOffset SharedAt, DateTimeOffset UpdatedAt);
public record InviteMemberRequest(Guid InviteeAuthUserId, string? Message, DateTimeOffset? ExpiresAt);
public record AddPartyResourceRequest(string Title, IReadOnlyDictionary<string, object> Content, IReadOnlyList<string> Tags);