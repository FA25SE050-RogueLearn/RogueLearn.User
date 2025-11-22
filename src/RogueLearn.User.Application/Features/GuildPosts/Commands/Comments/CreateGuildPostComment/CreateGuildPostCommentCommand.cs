using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.CreateGuildPostComment;

public record CreateGuildPostCommentCommand(Guid GuildId, Guid PostId, Guid AuthorId, CreateGuildPostCommentRequest Request) : IRequest<CreateGuildPostCommentResponse>;