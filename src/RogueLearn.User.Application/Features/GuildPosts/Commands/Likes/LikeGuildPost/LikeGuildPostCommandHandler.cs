using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Likes.LikeGuildPost;

public class LikeGuildPostCommandHandler : IRequestHandler<LikeGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildPostLikeRepository _likeRepository;
    private readonly IGuildMemberRepository _memberRepository;
    private readonly INotificationRepository _notificationRepository;

    public LikeGuildPostCommandHandler(
        IGuildPostRepository postRepository,
        IGuildPostLikeRepository likeRepository,
        IGuildMemberRepository memberRepository,
        INotificationRepository notificationRepository)
    {
        _postRepository = postRepository;
        _likeRepository = likeRepository;
        _memberRepository = memberRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<Unit> Handle(LikeGuildPostCommand request, CancellationToken cancellationToken)
    {
        var membership = await _memberRepository.GetMemberAsync(request.GuildId, request.UserId, cancellationToken);
        if (membership is null)
        {
            throw new UnauthorizedException();
        }

        var post = await _postRepository.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        if (post.IsLocked)
        {
            throw new ForbiddenException("Post is locked");
        }

        var existing = await _likeRepository.GetByPostAndUserAsync(request.PostId, request.UserId, cancellationToken);
        if (existing is not null)
        {
            return Unit.Value;
        }

        var like = new GuildPostLike
        {
            PostId = request.PostId,
            UserId = request.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _likeRepository.AddAsync(like, cancellationToken);

        post.LikeCount += 1;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _postRepository.UpdateAsync(post, cancellationToken);

        if (post.AuthorId != request.UserId)
        {
            await _notificationRepository.AddAsync(new Notification
            {
                AuthUserId = post.AuthorId,
                Type = Domain.Enums.NotificationType.System,
                Title = "New like on your guild post",
                Message = "Your post received a new like.",
                CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }

        return Unit.Value;
    }
}