namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Defines a plugin specifically for extracting the main curriculum structure.
/// </summary>
public interface ICurriculumExtractionPlugin
{
    Task<string> ExtractCurriculumJsonAsync(string rawText, CancellationToken cancellationToken = default);
}