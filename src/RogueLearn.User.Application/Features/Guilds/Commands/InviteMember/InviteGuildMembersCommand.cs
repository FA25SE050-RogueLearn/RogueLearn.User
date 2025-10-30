using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;

public record InviteTarget(Guid? UserId, string? Email);

public record InviteGuildMembersCommand(Guid GuildId, Guid InviterAuthUserId, IReadOnlyList<InviteTarget> Targets, string? Message)
    : IRequest<InviteGuildMembersResponse>;

public record InviteGuildMembersResponse(IReadOnlyList<Guid> InvitationIds);