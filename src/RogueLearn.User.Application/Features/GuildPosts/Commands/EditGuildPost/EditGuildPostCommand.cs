using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.EditGuildPost;

public record EditGuildPostCommand(Guid GuildId, Guid PostId, Guid AuthorAuthUserId, EditGuildPostRequest Request) : IRequest<Unit>;