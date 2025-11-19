// RogueLearn.User/src/RogueLearn.User.Application/Plugins/IQuestGenerationPlugin.cs

using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Interface for quest step generation plugin using AI.
/// Generates activities for a SINGLE week per call.
/// This plugin is called multiple times (once per week) to generate all quest steps.
/// </summary>
public interface IQuestGenerationPlugin
{
    /// <summary>
    /// Generates quest steps for a SINGLE week using AI.
    /// Called once per week to generate all quest steps sequentially.
    /// </summary>
    /// <param name="syllabusJson">The complete syllabus content as JSON</param>
    /// <param name="userContext">User/class context for personalization</param>
    /// <param name="relevantSkills">List of skills linked to the subject</param>
    /// <param name="subjectName">Name of the subject</param>
    /// <param name="courseDescription">Course description for context</param>
    /// <param name="weekNumber">The current week number (1-based)</param>
    /// <param name="totalWeeks">Total number of weeks to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON response with activities array: { "activities": [...] }</returns>
    Task<string?> GenerateQuestStepsJsonAsync(
        string syllabusJson,
        string userContext,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription,
        int weekNumber,
        int totalWeeks,
        CancellationToken cancellationToken = default);
}
