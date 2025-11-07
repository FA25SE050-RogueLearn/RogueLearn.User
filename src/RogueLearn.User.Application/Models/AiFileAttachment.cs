using System.IO;

namespace RogueLearn.User.Application.Models;

/// <summary>
/// Represents a file attachment sent to AI services.
/// </summary>
public sealed class AiFileAttachment
{
    /// <summary>
    /// Optional stream reference to the file. Preferred for large files or avoiding byte[] copies across layers.
    /// </summary>
    public Stream? Stream { get; init; }

    /// <summary>
    /// Optional raw bytes of the file. If not provided, consumers should read from <see cref="Stream"/>.
    /// </summary>
    public byte[]? Bytes { get; init; }

    /// <summary>
    /// MIME type of the file (e.g., application/pdf, text/plain).
    /// </summary>
    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>
    /// Original file name, used for heuristics and logging.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Optional known length of the file.
    /// </summary>
    public long? ProvidedLength { get; init; }

    /// <summary>
    /// Best-effort size of the file in bytes.
    /// </summary>
    public long Length =>
        Bytes?.LongLength ??
        ProvidedLength ??
        (Stream?.CanSeek == true ? Stream.Length : 0L);
}