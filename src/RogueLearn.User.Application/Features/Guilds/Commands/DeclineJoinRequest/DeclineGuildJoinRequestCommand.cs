using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.DeclineJoinRequest;

public record DeclineGuildJoinRequestCommand(Guid GuildId, Guid RequestId, Guid ActorAuthUserId) : IRequest<Unit>;