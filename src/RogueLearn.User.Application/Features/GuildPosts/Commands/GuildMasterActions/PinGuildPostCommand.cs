using MediatR;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.GuildMasterActions;

public record PinGuildPostCommand(Guid GuildId, Guid PostId) : IRequest<Unit>;

public record UnpinGuildPostCommand(Guid GuildId, Guid PostId) : IRequest<Unit>;

public record LockGuildPostCommand(Guid GuildId, Guid PostId) : IRequest<Unit>;

public record UnlockGuildPostCommand(Guid GuildId, Guid PostId) : IRequest<Unit>;