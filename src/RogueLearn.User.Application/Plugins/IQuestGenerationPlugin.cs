// src/RogueLearn.User.Application/Plugins/IQuestGenerationPlugin.cs
using System.Threading;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Interface for quest step generation plugin using AI.
/// Supports the "Master Quest" architecture by executing prompts that generate parallel difficulty tracks.
/// </summary>
public interface IQuestGenerationPlugin
{
    /// <summary>
    /// Executes a raw prompt to generate Master Quest steps with 3 difficulty variants.
    /// This decouples the prompt logic (now in QuestStepsPromptBuilder) from the AI execution logic.
    /// </summary>
    /// <param name="prompt">The fully constructed prompt string asking for 3-lane JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw JSON response from the AI, containing 'standard', 'supportive', and 'challenging' keys.</returns>
    Task<string?> GenerateFromPromptAsync(string prompt, CancellationToken cancellationToken = default);
}