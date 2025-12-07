using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.CreateGuildPostComment;

public class CreateGuildPostCommentCommand : IRequest<CreateGuildPostCommentResponse>
{
    public Guid GuildId { get; init; }
    public Guid PostId { get; init; }
    public Guid AuthorId { get; init; }
    public CreateGuildPostCommentRequest Request { get; init; } = new();
}
