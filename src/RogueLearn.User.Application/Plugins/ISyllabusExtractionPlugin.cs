// RogueLearn.User/src/RogueLearn.User.Application/Plugins/ISyllabusExtractionPlugin.cs
namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Defines a plugin specifically for extracting the detailed content of a single subject syllabus.
/// </summary>
public interface ISyllabusExtractionPlugin
{
    Task<string> ExtractSyllabusJsonAsync(string rawText, CancellationToken cancellationToken = default);
}