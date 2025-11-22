using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.EditGuildPostComment;

public record EditGuildPostCommentCommand(Guid GuildId, Guid PostId, Guid CommentId, Guid AuthorId, EditGuildPostCommentRequest Request) : IRequest<Unit>;