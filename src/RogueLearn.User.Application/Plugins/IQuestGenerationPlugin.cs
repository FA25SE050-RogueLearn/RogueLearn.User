using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Defines the contract for a plugin that generates interactive quest steps from syllabus content.
/// </summary>
public interface IQuestGenerationPlugin
{
    /// <summary>
    /// Generates a list of QuestStep entities in JSON format based on syllabus content and user context.
    /// </summary>
    /// <param name="syllabusJson">The structured syllabus content as a JSON string.</param>
    /// <param name="userContext">A string describing the user's career goals for personalization.</param>
    /// <param name="relevantSkills">A list of pre-approved skills for the AI to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A JSON string representing an array of quest steps, or null if generation fails.</returns>
    // MODIFICATION: Changed return type to Task<string?> to match the implementation's nullability, resolving the warning.
    Task<string?> GenerateQuestStepsJsonAsync(string syllabusJson, string userContext, List<Skill> relevantSkills, CancellationToken cancellationToken = default);
}
