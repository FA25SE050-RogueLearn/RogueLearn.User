using MediatR;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Likes.LikeGuildPost;

public record LikeGuildPostCommand(Guid GuildId, Guid PostId, Guid UserId) : IRequest<Unit>;