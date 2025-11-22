using MediatR;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;

namespace RogueLearn.User.Application.Features.GuildPosts.Commands.Images;

public record UploadGuildPostImagesCommand(Guid GuildId, Guid PostId, Guid AuthUserId, IReadOnlyList<GuildPostImageUpload> Images) : IRequest<UploadGuildPostImagesResponse>;

public record UploadGuildPostImagesResponse
{
    public IReadOnlyList<string> ImageUrls { get; init; } = Array.Empty<string>();
}