using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ApproveJoinRequest;

public record ApproveGuildJoinRequestCommand(Guid GuildId, Guid RequestId, Guid ActorAuthUserId) : IRequest<Unit>;