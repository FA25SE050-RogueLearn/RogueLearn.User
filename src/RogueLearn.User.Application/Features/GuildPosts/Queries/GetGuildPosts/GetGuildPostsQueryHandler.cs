using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPosts;

public class GetGuildPostsQueryHandler : IRequestHandler<GetGuildPostsQuery, IEnumerable<GuildPostDto>>
{
    private readonly IGuildPostRepository _repo;

    public GetGuildPostsQueryHandler(IGuildPostRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<GuildPostDto>> Handle(GetGuildPostsQuery request, CancellationToken cancellationToken)
    {
        var posts = await _repo.GetByGuildAsync(request.GuildId, request.Tag, request.AuthorId, request.Pinned, request.Search, request.Page, request.Size, cancellationToken);
        return posts.Select(p => new GuildPostDto
        {
            Id = p.Id,
            GuildId = p.GuildId,
            AuthorId = p.AuthorId,
            Title = p.Title,
            Content = p.Content,
            Tags = p.Tags,
            Attachments = p.Attachments,
            IsPinned = p.IsPinned,
            IsLocked = p.IsLocked,
            IsAnnouncement = p.IsAnnouncement,
            Status = p.Status,
            CommentCount = p.CommentCount,
            LikeCount = p.LikeCount,
            EmojiCounts = new Dictionary<string, int>(),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        });
    }
}