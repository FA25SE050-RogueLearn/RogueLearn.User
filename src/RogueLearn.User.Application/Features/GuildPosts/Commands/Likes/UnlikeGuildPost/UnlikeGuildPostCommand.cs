using MediatR;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Likes.UnlikeGuildPost;

public record UnlikeGuildPostCommand(Guid GuildId, Guid PostId, Guid UserId) : IRequest<Unit>;