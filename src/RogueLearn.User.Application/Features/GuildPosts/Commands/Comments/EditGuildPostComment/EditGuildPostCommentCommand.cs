using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.EditGuildPostComment;

public class EditGuildPostCommentCommand : IRequest<Unit>
{
    public Guid GuildId { get; init; }
    public Guid PostId { get; init; }
    public Guid CommentId { get; init; }
    public Guid AuthorId { get; init; }
    public EditGuildPostCommentRequest Request { get; init; } = new();
}
