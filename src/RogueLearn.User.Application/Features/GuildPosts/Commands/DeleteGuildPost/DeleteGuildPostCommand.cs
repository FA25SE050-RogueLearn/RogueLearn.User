using MediatR;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.DeleteGuildPost;

public record DeleteGuildPostCommand(Guid GuildId, Guid PostId, Guid RequesterAuthUserId, bool Force = false) : IRequest<Unit>;