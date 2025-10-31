using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.DeleteGuildPost;

public class DeleteGuildPostCommandHandler : IRequestHandler<DeleteGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _postRepository;

    public DeleteGuildPostCommandHandler(IGuildPostRepository postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<Unit> Handle(DeleteGuildPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.GuildId, request.PostId, cancellationToken)
            ?? throw new NotFoundException("GuildPost", request.PostId.ToString());

        if (!request.Force)
        {
            if (post.AuthorId != request.RequesterAuthUserId)
            {
                throw new ForbiddenException("Cannot delete another user's post");
            }

            if (post.IsLocked)
            {
                throw new ForbiddenException("Post is locked by admin");
            }
        }

        await _postRepository.DeleteAsync(post.Id, cancellationToken);
        return Unit.Value;
    }
}