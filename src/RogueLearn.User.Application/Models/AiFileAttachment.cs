namespace RogueLearn.User.Application.Models;

public sealed class AiFileAttachment
{
    public Stream? Stream { get; init; }
    public byte[]? Bytes { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
    public string FileName { get; init; } = string.Empty;
    public long? ProvidedLength { get; init; }
    public long Length =>
        Bytes?.LongLength ??
        ProvidedLength ??
        (Stream?.CanSeek == true ? Stream.Length : 0L);
}