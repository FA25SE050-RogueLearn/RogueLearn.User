using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.AcceptInvitation;

public record AcceptGuildInvitationCommand(Guid GuildId, Guid InvitationId, Guid AuthUserId) : IRequest<Unit>;