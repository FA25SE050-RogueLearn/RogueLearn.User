using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.EditGuildPostComment;

public class EditGuildPostCommentCommandHandler : IRequestHandler<EditGuildPostCommentCommand, Unit>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildPostCommentRepository _commentRepository;

    public EditGuildPostCommentCommandHandler(IGuildPostRepository postRepository, IGuildPostCommentRepository commentRepository)
    {
        _postRepository = postRepository;
        _commentRepository = commentRepository;
    }

    public async Task<Unit> Handle(EditGuildPostCommentCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        if (post.IsLocked)
        {
            throw new ForbiddenException("Post is locked");
        }

        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken) ?? throw new NotFoundException("GuildPostComment", request.CommentId.ToString());
        if (comment.PostId != request.PostId || comment.AuthorId != request.AuthorId)
        {
            throw new ForbiddenException("Not allowed");
        }

        comment.Content = request.Request.Content;
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        return Unit.Value;
    }
}