using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.CreateGuildPost;

public class CreateGuildPostCommandHandler : IRequestHandler<CreateGuildPostCommand, CreateGuildPostResponse>
{
    private readonly IGuildPostRepository _postRepository;
    private readonly IGuildMemberRepository _memberRepository;
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildPostImageStorage _imageStorage;

    public CreateGuildPostCommandHandler(IGuildPostRepository postRepository, IGuildMemberRepository memberRepository, IGuildRepository guildRepository, IGuildPostImageStorage imageStorage)
    {
        _postRepository = postRepository;
        _memberRepository = memberRepository;
        _guildRepository = guildRepository;
        _imageStorage = imageStorage;
    }

    public async Task<CreateGuildPostResponse> Handle(CreateGuildPostCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new NotFoundException("Guild", request.GuildId.ToString());

        var membership = await _memberRepository.GetMemberAsync(request.GuildId, request.AuthorAuthUserId, cancellationToken)
            ?? throw new ForbiddenException("Not a guild member");

        if (membership.Status != MemberStatus.Active)
        {
            throw new ForbiddenException("Inactive membership");
        }

        var status = guild.RequiresApproval ? GuildPostStatus.pending : GuildPostStatus.published;

        var post = new GuildPost
        {
            Id = Guid.NewGuid(),
            GuildId = request.GuildId,
            AuthorId = request.AuthorAuthUserId,
            Title = request.Request.Title,
            Content = request.Request.Content,
            Tags = request.Request.Tags,
            Attachments = request.Request.Attachments,
            IsPinned = false,
            IsLocked = false,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

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
            post.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _postRepository.AddAsync(post, cancellationToken);

        // TODO: Notifications to guild members on new posts (future)

        return new CreateGuildPostResponse { PostId = post.Id };
    }
}