using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Queries.GetPinnedGuildPosts;

public class GetPinnedGuildPostsQueryHandler : IRequestHandler<GetPinnedGuildPostsQuery, IEnumerable<GuildPostDto>>
{
    private readonly IGuildPostRepository _repo;
    public GetPinnedGuildPostsQueryHandler(IGuildPostRepository repo) => _repo = repo;

    public async Task<IEnumerable<GuildPostDto>> Handle(GetPinnedGuildPostsQuery request, CancellationToken cancellationToken)
    {
        var posts = await _repo.GetPinnedByGuildAsync(request.GuildId, cancellationToken);
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
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        });
    }
}