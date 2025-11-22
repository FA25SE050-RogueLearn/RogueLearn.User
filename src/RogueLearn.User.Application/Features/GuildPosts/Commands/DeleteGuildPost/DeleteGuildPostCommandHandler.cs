using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.DeleteGuildPost;

public class DeleteGuildPostCommandHandler : IRequestHandler<DeleteGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildPostImageStorage _imageStorage;

    public DeleteGuildPostCommandHandler(IGuildPostRepository postRepository, IGuildPostImageStorage imageStorage)
    {
        _postRepository = postRepository;
        _imageStorage = imageStorage;
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

        if (post.Attachments is not null && post.Attachments.TryGetValue("images", out var imgs) && imgs is IEnumerable<object> list)
        {
            var urls = list.Select(x => x?.ToString()).Where(s => !string.IsNullOrWhiteSpace(s))!.Cast<string>();
            await _imageStorage.DeleteByUrlsAsync(urls, cancellationToken);
        }

        await _postRepository.DeleteAsync(post.Id, cancellationToken);
        return Unit.Value;
    }
}