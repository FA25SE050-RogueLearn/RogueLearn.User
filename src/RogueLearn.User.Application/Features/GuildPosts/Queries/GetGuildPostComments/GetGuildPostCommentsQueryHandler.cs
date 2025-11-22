using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostComments;

public class GetGuildPostCommentsQueryHandler : IRequestHandler<GetGuildPostCommentsQuery, IEnumerable<GuildPostCommentDto>>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildPostCommentRepository _commentRepository;

    public GetGuildPostCommentsQueryHandler(IGuildPostRepository postRepository, IGuildPostCommentRepository commentRepository)
    {
        _postRepository = postRepository;
        _commentRepository = commentRepository;
    }

    public async Task<IEnumerable<GuildPostCommentDto>> Handle(GetGuildPostCommentsQuery request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());
        var comments = await _commentRepository.GetByPostAsync(post.Id, request.Page, request.Size, cancellationToken);

        var list = new List<GuildPostCommentDto>();
        foreach (var c in comments)
        {
            var replyCount = await _commentRepository.CountRepliesAsync(c.Id, cancellationToken);
            list.Add(new GuildPostCommentDto
            {
                Id = c.Id,
                PostId = c.PostId,
                AuthorId = c.AuthorId,
                Content = c.DeletedAt.HasValue ? null : c.Content,
                ParentCommentId = c.ParentCommentId,
                IsDeleted = c.DeletedAt.HasValue,
                ReplyCount = replyCount,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });
        }

        return list;
    }
}