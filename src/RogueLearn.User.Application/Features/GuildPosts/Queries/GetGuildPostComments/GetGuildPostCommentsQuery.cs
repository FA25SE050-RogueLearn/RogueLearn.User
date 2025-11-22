using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostComments;

public record GetGuildPostCommentsQuery(Guid GuildId, Guid PostId, int Page = 1, int Size = 20, string? Sort = null) : IRequest<IEnumerable<GuildPostCommentDto>>;