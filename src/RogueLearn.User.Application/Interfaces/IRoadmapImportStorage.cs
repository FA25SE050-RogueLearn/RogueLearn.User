namespace RogueLearn.User.Application.Interfaces;

public interface IRoadmapImportStorage
{
    /// <summary>
    /// Saves a PDF attachment associated with a roadmap import to Supabase storage.
    /// </summary>
    /// <param name="bucketName">Target storage bucket (e.g., "roadmap-imports")</param>
    /// <param name="className">Display name of the class, used to derive folder path</param>
    /// <param name="rawTextHash">Hash that identifies the import session/content (used in filename)</param>
    /// <param name="pdfStream">Stream of the PDF file content</param>
    /// <param name="originalFileName">Original PDF filename (for metadata/logging)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SavePdfAttachmentAsync(
        string bucketName,
        string className,
        string rawTextHash,
        Stream pdfStream,
        string originalFileName,
        CancellationToken cancellationToken = default);
}