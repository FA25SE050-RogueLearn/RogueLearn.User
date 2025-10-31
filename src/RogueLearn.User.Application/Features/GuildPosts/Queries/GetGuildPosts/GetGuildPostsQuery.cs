using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPosts;

public record GetGuildPostsQuery(
    Guid GuildId,
    string? Tag,
    Guid? AuthorId,
    bool? Pinned,
    string? Search,
    int Page = 1,
    int Size = 20
) : IRequest<IEnumerable<GuildPostDto>>;