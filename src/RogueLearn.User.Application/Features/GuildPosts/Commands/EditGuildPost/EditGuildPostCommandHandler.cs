using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.EditGuildPost;

public class EditGuildPostCommandHandler : IRequestHandler<EditGuildPostCommand, Unit>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildPostImageStorage _imageStorage;

    public EditGuildPostCommandHandler(IGuildPostRepository postRepository, IGuildPostImageStorage imageStorage)
    {
        _postRepository = postRepository;
        _imageStorage = imageStorage;
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

        if (request.Request.Images is not null && request.Request.Images.Count > 0)
        {
            var files = request.Request.Images.Select(i => (i.Content, i.ContentType, i.FileName));
            var urls = await _imageStorage.SaveImagesAsync(request.GuildId, post.Id, files, cancellationToken);
            var attachments = post.Attachments ?? new Dictionary<string, object>();
            var images = new List<object>();
            if (attachments.TryGetValue("images", out var existing) && existing is IEnumerable<object> e)
            {
                images = e.ToList();
            }
            foreach (var url in urls)
            {
                images.Add(url);
            }
            attachments["images"] = images;
            post.Attachments = attachments;
        }

        await _postRepository.UpdateAsync(post, cancellationToken);
        return Unit.Value;
    }
}