using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Images;

public class UploadGuildPostImagesCommandHandler : IRequestHandler<UploadGuildPostImagesCommand, UploadGuildPostImagesResponse>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildPostImageStorage _storage;

    public UploadGuildPostImagesCommandHandler(IGuildPostRepository postRepository, IGuildMemberRepository memberRepository, IGuildPostImageStorage storage)
    {
        _postRepository = postRepository;
        _memberRepository = memberRepository;
        _storage = storage;
    }

    public async Task<UploadGuildPostImagesResponse> Handle(UploadGuildPostImagesCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.GuildId, request.PostId, cancellationToken) ?? throw new NotFoundException("GuildPost", request.PostId.ToString());

        var membership = await _memberRepository.GetMemberAsync(request.GuildId, request.AuthUserId, cancellationToken);
        if (membership is null)
        {
            throw new ForbiddenException("Not a guild member");
        }

        if (post.AuthorId != request.AuthUserId)
        {
            throw new ForbiddenException("Cannot modify another user's post");
        }

        if (post.IsLocked)
        {
            throw new ForbiddenException("Post is locked");
        }

        var files = request.Images.Select(i => (i.Content, i.ContentType, i.FileName));
        var urls = await _storage.SaveImagesAsync(request.GuildId, request.PostId, files, cancellationToken);

        var attachments = post.Attachments ?? new Dictionary<string, object>();
        List<object> images;
        if (attachments.TryGetValue("images", out var existing) && existing is IEnumerable<object> e)
        {
            images = e.ToList();
        }
        else
        {
            images = new List<object>();
        }
        foreach (var url in urls)
        {
            images.Add(url);
        }
        attachments["images"] = images;
        post.Attachments = attachments;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _postRepository.UpdateAsync(post, cancellationToken);

        return new UploadGuildPostImagesResponse { ImageUrls = urls };
    }
}