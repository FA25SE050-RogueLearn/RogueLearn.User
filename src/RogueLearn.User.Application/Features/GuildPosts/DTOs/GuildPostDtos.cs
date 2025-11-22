namespace RogueLearn.User.Application.Features.GuildPosts.DTOs;

using RogueLearn.User.Domain.Enums;

public record GuildPostDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public Guid AuthorId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string[]? Tags { get; init; }
    public Dictionary<string, object>? Attachments { get; init; }
    public bool IsPinned { get; init; }
    public bool IsLocked { get; init; }
    public GuildPostStatus Status { get; init; }
    public int CommentCount { get; init; }
    public int LikeCount { get; init; }
    public Dictionary<string, int>? EmojiCounts { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public record CreateGuildPostRequest
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string[]? Tags { get; init; }
    public Dictionary<string, object>? Attachments { get; init; }
    public IReadOnlyList<GuildPostImageUpload>? Images { get; init; }
}

public record CreateGuildPostResponse
{
    public Guid PostId { get; init; }
}

public record EditGuildPostRequest
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string[]? Tags { get; init; }
    public Dictionary<string, object>? Attachments { get; init; }
    public IReadOnlyList<GuildPostImageUpload>? Images { get; init; }
}