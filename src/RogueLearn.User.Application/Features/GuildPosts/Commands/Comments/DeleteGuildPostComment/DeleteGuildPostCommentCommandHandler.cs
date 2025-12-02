using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.DeleteGuildPostComment;

public class DeleteGuildPostCommentCommandHandler : IRequestHandler<DeleteGuildPostCommentCommand, Unit>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildPostCommentRepository _commentRepository;

    public DeleteGuildPostCommentCommandHandler(IGuildPostRepository postRepository, IGuildPostCommentRepository commentRepository)
    {
        _postRepository = postRepository;
        _commentRepository = commentRepository;
    }

    public async Task<Unit> Handle(DeleteGuildPostCommentCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken) ?? throw new NotFoundException("GuildPostComment", request.CommentId.ToString());

        var isOwner = comment.AuthorId == request.RequesterId;
        if (!isOwner)
        {
            throw new ForbiddenException("Not allowed");
        }

        comment.DeletedAt = DateTimeOffset.UtcNow;
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        await _commentRepository.UpdateAsync(comment, cancellationToken);

        post.CommentCount = Math.Max(0, post.CommentCount - 1);
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _postRepository.UpdateAsync(post, cancellationToken);

        return Unit.Value;
    }
}