namespace RogueLearn.User.Application.Models;

public class TagSuggestionDto
{ 
    public string Label { get; set; } = string.Empty;  
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? MatchedTagId { get; set; }
    public string? MatchedTagName { get; set; }
    public bool IsExisting => MatchedTagId.HasValue;
}

public class SuggestNoteTagsResponse
{
    public IReadOnlyList<TagSuggestionDto> Suggestions { get; set; } = Array.Empty<TagSuggestionDto>();
}

public class CommitNoteTagSelectionsResponse
{
    public Guid NoteId { get; set; }
    public IReadOnlyList<Guid> AddedTagIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<CreatedTagDto> CreatedTags { get; set; } = Array.Empty<CreatedTagDto>();
    public int TotalTagsAssigned { get; set; }
}

public class CreatedTagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}