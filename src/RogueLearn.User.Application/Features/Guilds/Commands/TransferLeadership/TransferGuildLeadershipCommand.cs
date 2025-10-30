using MediatR;

namespace RogueLearn.User.Application.Features.Guilds.Commands.TransferLeadership;

public record TransferGuildLeadershipCommand(Guid GuildId, Guid ToUserId) : IRequest<Unit>;