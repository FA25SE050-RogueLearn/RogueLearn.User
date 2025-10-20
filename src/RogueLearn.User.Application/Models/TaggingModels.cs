namespace RogueLearn.User.Application.Models;

/// <summary>
/// Represents a single suggested tag, with optional mapping to an existing user tag.
/// </summary>
public class TagSuggestionDto
{
    /// <summary>
    /// The human-readable label of the suggested tag.
    /// </summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>
    /// Confidence score from the AI (0.0-1.0).
    /// </summary>
    public double Confidence { get; set; }
    /// <summary>
    /// Short description of why the tag was suggested.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// If matched to an existing tag, the ID of that tag.
    /// </summary>
    public Guid? MatchedTagId { get; set; }
    /// <summary>
    /// If matched to an existing tag, the name of that tag.
    /// </summary>
    public string? MatchedTagName { get; set; }
    /// <summary>
    /// Indicates if the suggestion maps to an existing tag (true) or is new (false).
    /// </summary>
    public bool IsExisting => MatchedTagId.HasValue;
}

/// <summary>
/// Response model containing AI tag suggestions.
/// </summary>
public class SuggestNoteTagsResponse
{
    /// <summary>
    /// The list of suggested tags.
    /// </summary>
    public IReadOnlyList<TagSuggestionDto> Suggestions { get; set; } = Array.Empty<TagSuggestionDto>();
}

/// <summary>
/// Response model after committing selected tags to a note.
/// </summary>
public class CommitNoteTagSelectionsResponse
{
    /// <summary>
    /// The note ID the tags were applied to.
    /// </summary>
    public Guid NoteId { get; set; }
    /// <summary>
    /// IDs of tags that were assigned (including newly created ones).
    /// </summary>
    public IReadOnlyList<Guid> AddedTagIds { get; set; } = Array.Empty<Guid>();
    /// <summary>
    /// Details of any new tags that were created.
    /// </summary>
    public IReadOnlyList<CreatedTagDto> CreatedTags { get; set; } = Array.Empty<CreatedTagDto>();
    /// <summary>
    /// Total number of tags now assigned to the note.
    /// </summary>
    public int TotalTagsAssigned { get; set; }
}

/// <summary>
/// DTO describing a newly created tag.
/// </summary>
public class CreatedTagDto
{
    /// <summary>
    /// The created tag ID.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// The created tag name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}