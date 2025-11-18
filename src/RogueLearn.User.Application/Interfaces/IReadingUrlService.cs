// RogueLearn.User/src/RogueLearn.User.Application/Interfaces/IReadingUrlService.cs
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Interfaces;

public interface IReadingUrlService
{
    /// <summary>
    /// Find and validate a URL for the given topic with category-aware filtering.
    /// </summary>
    /// <param name="topic">The session topic to search for</param>
    /// <param name="readings">Existing readings from syllabus</param>
    /// <param name="subjectContext">Subject name and technology stack for relevance filtering</param>
    /// <param name="category">Subject category (Programming, Vietnamese Politics, History, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Valid URL or null if none found</returns>
    Task<string?> GetValidUrlForTopicAsync(
        string topic,
        IEnumerable<string> readings,
        string? subjectContext = null,
        SubjectCategory category = SubjectCategory.General,  // ⭐ ADD THIS PARAMETER
        CancellationToken cancellationToken = default);
}
