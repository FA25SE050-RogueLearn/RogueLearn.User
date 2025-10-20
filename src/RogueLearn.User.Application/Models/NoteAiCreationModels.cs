using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Models;

public class CreateNoteWithAiTagsResponse
{
    public Guid NoteId { get; set; }
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<TagSuggestionDto> Suggestions { get; set; } = Array.Empty<TagSuggestionDto>();
    public IReadOnlyList<Guid> AppliedTagIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<CreatedTagDto> CreatedTags { get; set; } = Array.Empty<CreatedTagDto>();
    public int TotalTagsAssigned { get; set; }
}