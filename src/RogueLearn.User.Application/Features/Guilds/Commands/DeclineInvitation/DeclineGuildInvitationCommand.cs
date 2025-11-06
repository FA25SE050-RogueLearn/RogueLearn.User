using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.DeclineInvitation;

public record DeclineGuildInvitationCommand(Guid GuildId, Guid InvitationId, Guid AuthUserId) : IRequest<Unit>;