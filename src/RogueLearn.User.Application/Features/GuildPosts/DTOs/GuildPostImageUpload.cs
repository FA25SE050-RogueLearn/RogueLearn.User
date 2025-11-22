namespace RogueLearn.User.Application.Features.GuildPosts.DTOs;

public record GuildPostImageUpload(byte[] Content, string? ContentType, string? FileName);