namespace RogueLearn.User.Domain.Enums;

/// <summary>
/// Represents the administrative status of a Master Quest.
/// This controls visibility to students.
/// </summary>
public enum QuestStatus
{
    /// <summary>
    /// Content is being generated or reviewed. Not visible to students.
    /// </summary>
    Draft,

    /// <summary>
    /// Content is approved and live. Visible for students to attempt.
    /// </summary>
    Published,

    /// <summary>
    /// Content is retired. Hidden from new attempts, but historical data remains.
    /// </summary>
    Archived
}