using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Queries.GetPinnedGuildPosts;

public record GetPinnedGuildPostsQuery(Guid GuildId) : IRequest<IEnumerable<GuildPostDto>>;