using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Interfaces;

public interface IFileTextExtractor
{
    /// <summary>
    /// Extract textual content from an uploaded document stream.
    /// Supports PDF, TXT, and DOCX. Uses contentType and fileName hints.
    /// </summary>
    Task<string> ExtractTextAsync(Stream fileStream, string contentType, string fileName, CancellationToken cancellationToken = default);
}