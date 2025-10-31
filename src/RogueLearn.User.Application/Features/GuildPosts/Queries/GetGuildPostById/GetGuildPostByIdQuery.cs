using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostById;

public record GetGuildPostByIdQuery(Guid GuildId, Guid PostId) : IRequest<GuildPostDto?>;