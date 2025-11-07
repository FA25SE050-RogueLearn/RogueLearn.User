namespace RogueLearn.User.Application.Options;

/// <summary>
/// Cross-cutting options for AI file processing.
/// </summary>
public class AiFileProcessingOptions
{
    /// <summary>
    /// When true, the system will fall back to local text extraction if the AI provider
    /// does not support direct file inputs or returns an error. Intended to be disabled
    /// once direct file ingestion is supported end-to-end.
    /// </summary>
    public bool EnableLocalTextFallback { get; set; } = true;

    /// <summary>
    /// Maximum file size in megabytes allowed for processing.
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 50;

    /// <summary>
    /// Allowed MIME types for uploads.
    /// </summary>
    public string[] AllowedMimeTypes { get; set; } = new[]
    {
        "application/pdf",
        "text/plain",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "application/vnd.openxmlformats-officedocument.presentationml.presentation" // .pptx
    };

    /// <summary>
    /// Whether only a single file per request is supported. Kept for future-proofing if batch ingest is introduced later.
    /// </summary>
    public bool SingleFileOnly { get; set; } = true;
}