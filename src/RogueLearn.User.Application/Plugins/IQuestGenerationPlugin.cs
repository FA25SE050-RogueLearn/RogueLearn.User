namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Interface for quest step generation plugin using AI.
/// Supports the "Master Quest" architecture by executing prompts that generate parallel difficulty tracks.
/// </summary>
public interface IQuestGenerationPlugin
{
    Task<string?> GenerateFromPromptAsync(string prompt, CancellationToken cancellationToken = default);
}