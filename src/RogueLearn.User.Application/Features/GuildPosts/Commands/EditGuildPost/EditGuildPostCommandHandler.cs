using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.EditGuildPost;

public class EditGuildPostCommandHandler : IRequestHandler<EditGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _postRepository;

    public EditGuildPostCommandHandler(IGuildPostRepository postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<Unit> Handle(EditGuildPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.GuildId, request.PostId, cancellationToken)
            ?? throw new NotFoundException("GuildPost", request.PostId.ToString());

        if (post.AuthorId != request.AuthorAuthUserId)
        {
            throw new ForbiddenException("Cannot edit another user's post");
        }

        if (post.IsLocked)
        {
            throw new ForbiddenException("Post is locked by admin");
        }

        post.Title = request.Request.Title;
        post.Content = request.Request.Content;
        post.Tags = request.Request.Tags;
        post.Attachments = request.Request.Attachments;
        post.UpdatedAt = DateTimeOffset.UtcNow;

        await _postRepository.UpdateAsync(post, cancellationToken);
        return Unit.Value;
    }
}