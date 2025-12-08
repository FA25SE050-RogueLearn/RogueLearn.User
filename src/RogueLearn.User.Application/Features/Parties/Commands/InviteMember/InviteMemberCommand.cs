using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.InviteMember;

public record InviteTarget(Guid? UserId, string? Email);

public record InviteMemberCommand(
    Guid PartyId,
    Guid InviterAuthUserId,
    IReadOnlyList<InviteTarget> Targets,
    string? Message,
    DateTimeOffset ExpiresAt,
    string? JoinLink = null,
    Guid? GameSessionId = null)
    : IRequest<Unit>;
