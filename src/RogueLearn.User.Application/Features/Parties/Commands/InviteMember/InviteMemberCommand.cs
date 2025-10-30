using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Commands.InviteMember;

public record InviteMemberCommand(Guid PartyId, Guid InviterAuthUserId, Guid InviteeAuthUserId, string? Message, DateTimeOffset ExpiresAt)
    : IRequest<PartyInvitationDto>;