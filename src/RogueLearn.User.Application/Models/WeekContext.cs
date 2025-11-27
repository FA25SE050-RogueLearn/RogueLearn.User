// src/RogueLearn.User.Application/Models/WeekContext.cs
namespace RogueLearn.User.Application.Models;

/// <summary>
/// Represents the aggregated context for a specific week's quest generation.
/// Used to decouple strict session-to-activity mapping.
/// </summary>
public class WeekContext
{
    public int WeekNumber { get; set; }
    public int TotalWeeks { get; set; }

    /// <summary>
    /// List of all topics that must be covered in this week.
    /// Aggregated from the individual sessions.
    /// </summary>
    public List<string> TopicsToCover { get; set; } = new();

    /// <summary>
    /// Pool of valid, unique URLs available for this week.
    /// </summary>
    public List<ValidResource> AvailableResources { get; set; } = new();
}

public class ValidResource
{
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The topic or session title where this URL was originally found, providing context for the AI.
    /// </summary>
    public string SourceContext { get; set; } = string.Empty;
}