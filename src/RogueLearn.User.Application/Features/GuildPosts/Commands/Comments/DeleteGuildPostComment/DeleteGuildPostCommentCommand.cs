using MediatR;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.DeleteGuildPostComment;

public record DeleteGuildPostCommentCommand(Guid GuildId, Guid PostId, Guid CommentId, Guid RequesterId, bool HardDelete = false) : IRequest<Unit>;