namespace RogueLearn.User.Application.Features.GuildPosts.DTOs;

public record GuildPostCommentDto
{
    public Guid Id { get; init; }
    public Guid PostId { get; init; }
    public Guid AuthorId { get; init; }
    public string? Content { get; init; }
    public Guid? ParentCommentId { get; init; }
    public bool IsDeleted { get; init; }
    public int ReplyCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public record CreateGuildPostCommentRequest
{
    public string Content { get; init; } = string.Empty;
    public Guid? ParentCommentId { get; init; }
}

public record CreateGuildPostCommentResponse
{
    public Guid CommentId { get; init; }
}

public record EditGuildPostCommentRequest
{
    public string Content { get; init; } = string.Empty;
}