using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.CreateGuildPost;

public class CreateGuildPostCommand : IRequest<CreateGuildPostResponse>
{
    public Guid GuildId { get; init; }
    public Guid AuthorAuthUserId { get; init; }
    public CreateGuildPostRequest Request { get; init; } = new();
}
