using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Defines a plugin specifically for generating constructive questions when they are missing from a syllabus.
/// </summary>
public interface IConstructiveQuestionGenerationPlugin
{
    Task<List<ConstructiveQuestion>> GenerateQuestionsAsync(List<SyllabusSessionDto> sessionSchedule, CancellationToken cancellationToken = default);
}