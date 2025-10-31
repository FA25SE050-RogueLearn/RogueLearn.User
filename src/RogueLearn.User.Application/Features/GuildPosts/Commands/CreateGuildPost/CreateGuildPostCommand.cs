using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.CreateGuildPost;

public record CreateGuildPostCommand(Guid GuildId, Guid AuthorAuthUserId, CreateGuildPostRequest Request) : IRequest<CreateGuildPostResponse>;