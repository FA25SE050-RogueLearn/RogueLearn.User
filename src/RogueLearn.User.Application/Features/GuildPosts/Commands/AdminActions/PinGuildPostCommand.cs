using MediatR;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.AdminActions;

public record PinGuildPostCommand(Guid GuildId, Guid PostId) : IRequest<Unit>;

public record UnpinGuildPostCommand(Guid GuildId, Guid PostId) : IRequest<Unit>;

public record LockGuildPostCommand(Guid GuildId, Guid PostId) : IRequest<Unit>;

public record UnlockGuildPostCommand(Guid GuildId, Guid PostId) : IRequest<Unit>;

public record ApproveGuildPostCommand(Guid GuildId, Guid PostId, string? Note) : IRequest<Unit>;

public record RejectGuildPostCommand(Guid GuildId, Guid PostId, string? Reason) : IRequest<Unit>;