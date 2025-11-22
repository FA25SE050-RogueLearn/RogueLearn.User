namespace RogueLearn.User.Application.Interfaces;

public interface IGuildPostImageStorage
{
    Task<IReadOnlyList<string>> SaveImagesAsync(
        Guid guildId,
        Guid postId,
        IEnumerable<(byte[] Content, string? ContentType, string? FileName)> files,
        CancellationToken cancellationToken = default);

    Task DeleteByUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
}