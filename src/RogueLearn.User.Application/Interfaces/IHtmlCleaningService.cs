namespace RogueLearn.User.Application.Interfaces;

/// <summary>
/// Defines a service for cleaning and pre-processing raw HTML content
/// to extract meaningful text for AI analysis.
/// </summary>
public interface IHtmlCleaningService
{
    /// <summary>
    /// Extracts clean, structured text from a raw HTML string.
    /// </summary>
    /// <param name="rawHtml">The raw HTML content.</param>
    /// <returns>A cleaned string representation of the important text content.</returns>
    string ExtractCleanTextFromHtml(string rawHtml);
}