using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Likes.UnlikeGuildPost;

public class UnlikeGuildPostCommandHandler : IRequestHandler<UnlikeGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildPostLikeRepository _likeRepository;
    private readonly IGuildMemberRepository _memberRepository;

    public UnlikeGuildPostCommandHandler(IGuildPostRepository postRepository, IGuildPostLikeRepository likeRepository, IGuildMemberRepository memberRepository)
    {
        _postRepository = postRepository;
        _likeRepository = likeRepository;
        _memberRepository = memberRepository;
    }

    public async Task<Unit> Handle(UnlikeGuildPostCommand request, CancellationToken cancellationToken)
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
        if (existing is null)
        {
            return Unit.Value;
        }

        await _likeRepository.DeleteAsync(existing.Id, cancellationToken);
        post.LikeCount = Math.Max(0, post.LikeCount - 1);
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _postRepository.UpdateAsync(post, cancellationToken);

        return Unit.Value;
    }
}