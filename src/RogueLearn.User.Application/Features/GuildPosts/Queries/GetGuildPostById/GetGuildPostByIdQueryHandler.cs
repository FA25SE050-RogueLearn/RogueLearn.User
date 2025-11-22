using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostById;

public class GetGuildPostByIdQueryHandler : IRequestHandler<GetGuildPostByIdQuery, GuildPostDto?>
{
    private readonly IGuildPostRepository _repo;
    public GetGuildPostByIdQueryHandler(IGuildPostRepository repo) => _repo = repo;

    public async Task<GuildPostDto?> Handle(GetGuildPostByIdQuery request, CancellationToken cancellationToken)
    {
        var post = await _repo.GetByIdAsync(request.GuildId, request.PostId, cancellationToken);
        if (post is null) return null;
        return new GuildPostDto
        {
            Id = post.Id,
            GuildId = post.GuildId,
            AuthorId = post.AuthorId,
            Title = post.Title,
            Content = post.Content,
            Tags = post.Tags,
            Attachments = post.Attachments,
            IsPinned = post.IsPinned,
            IsLocked = post.IsLocked,
            Status = post.Status,
            CommentCount = post.CommentCount,
            LikeCount = post.LikeCount,
            EmojiCounts = new Dictionary<string, int>(),
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
    }
}