// RogueLearn.User/src/RogueLearn.User.Application/Interfaces/IReadingUrlService.cs
namespace RogueLearn.User.Application.Interfaces;

/// <summary>
/// Defines a service for sourcing valid URLs for reading materials,
/// prioritizing existing links and falling back to web search.
/// </summary>
public interface IReadingUrlService
{
    /// <summary>
    /// Gets a valid URL for a given topic and list of reading materials.
    /// </summary>
    /// <param name="topic">The topic of the reading material, used as a search query if needed.</param>
    /// <param name="readings">The list of reading materials from the syllabus, which may contain URLs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A valid URL string, or null if one cannot be found.</returns>
    Task<string?> GetValidUrlForTopicAsync(string topic, IEnumerable<string> readings, CancellationToken cancellationToken = default);
}