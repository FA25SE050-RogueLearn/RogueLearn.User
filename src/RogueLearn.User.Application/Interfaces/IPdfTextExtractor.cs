namespace RogueLearn.User.Application.Interfaces;

public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts plain text from a PDF stream.
    /// </summary>
    /// <param name="pdfStream">Input PDF stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted text content</returns>
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}