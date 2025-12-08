using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Images;

public class UploadGuildPostImagesCommand : IRequest<UploadGuildPostImagesResponse>
{
    public Guid GuildId { get; init; }
    public Guid PostId { get; init; }
    public Guid AuthUserId { get; init; }
    public IReadOnlyList<GuildPostImageUpload> Images { get; init; } = Array.Empty<GuildPostImageUpload>();
}

public class UploadGuildPostImagesResponse
{
    public IReadOnlyList<string> ImageUrls { get; init; } = Array.Empty<string>();
}