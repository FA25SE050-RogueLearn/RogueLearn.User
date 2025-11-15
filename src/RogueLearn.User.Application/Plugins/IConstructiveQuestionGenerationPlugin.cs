// RogueLearn.User/src/RogueLearn.User.Application/Plugins/IConstructiveQuestionGenerationPlugin.cs
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Defines a plugin specifically for generating constructive questions when they are missing from a syllabus.
/// </summary>
public interface IConstructiveQuestionGenerationPlugin
{
    /// <summary>
    /// Generates a list of constructive questions based on the topics and activities of a session schedule.
    /// </summary>
    /// <param name="sessionSchedule">The list of sessions from the syllabus content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of generated constructive questions.</returns>
    Task<List<ConstructiveQuestion>> GenerateQuestionsAsync(List<SyllabusSessionDto> sessionSchedule, CancellationToken cancellationToken = default);
}