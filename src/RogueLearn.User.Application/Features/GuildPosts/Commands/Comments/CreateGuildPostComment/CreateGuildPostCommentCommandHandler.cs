using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.CreateGuildPostComment;

public class CreateGuildPostCommentCommandHandler : IRequestHandler<CreateGuildPostCommentCommand, CreateGuildPostCommentResponse>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildPostCommentRepository _commentRepository;
    private readonly IGuildMemberRepository _memberRepository;
    private readonly INotificationRepository _notificationRepository;

    public CreateGuildPostCommentCommandHandler(IGuildPostRepository postRepository, IGuildPostCommentRepository commentRepository, IGuildMemberRepository memberRepository, INotificationRepository notificationRepository)
    {
        _postRepository = postRepository;
        _commentRepository = commentRepository;
        _memberRepository = memberRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<CreateGuildPostCommentResponse> Handle(CreateGuildPostCommentCommand request, CancellationToken cancellationToken)
    {
        var membership = await _memberRepository.GetMemberAsync(request.GuildId, request.AuthorId, cancellationToken);
        if (membership is null)
        {
            throw new UnauthorizedException();
        }

        var post = await _postRepository.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        if (post.IsLocked)
        {
            throw new ForbiddenException("Post is locked");
        }

        if (request.Request.ParentCommentId.HasValue)
        {
            var parent = await _commentRepository.GetByIdAsync(request.Request.ParentCommentId.Value, cancellationToken);
            if (parent is null || parent.PostId != request.PostId)
            {
                throw new BadRequestException("Invalid parentCommentId");
            }
        }

        var comment = new GuildPostComment
        {
            PostId = request.PostId,
            AuthorId = request.AuthorId,
            Content = request.Request.Content,
            ParentCommentId = request.Request.ParentCommentId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _commentRepository.AddAsync(comment, cancellationToken);

        post.CommentCount += 1;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _postRepository.UpdateAsync(post, cancellationToken);

        if (post.AuthorId != request.AuthorId)
        {
            await _notificationRepository.AddAsync(new Notification
            {
                AuthUserId = post.AuthorId,
                Type = RogueLearn.User.Domain.Enums.NotificationType.System,
                Title = "New comment on your guild post",
                Message = "Your post received a new comment.",
                CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }

        return new CreateGuildPostCommentResponse { CommentId = comment.Id };
    }
}