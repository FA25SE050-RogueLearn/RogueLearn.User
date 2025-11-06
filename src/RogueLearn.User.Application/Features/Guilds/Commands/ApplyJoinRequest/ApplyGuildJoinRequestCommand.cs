using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.ApplyJoinRequest;

public record ApplyGuildJoinRequestCommand(Guid GuildId, Guid AuthUserId, string? Message) : IRequest<Unit>;