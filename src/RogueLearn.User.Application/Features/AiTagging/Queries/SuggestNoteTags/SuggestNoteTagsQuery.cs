using MediatR;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTags;

public class SuggestNoteTagsQuery : IRequest<SuggestNoteTagsResponse>
{
    public Guid AuthUserId { get; set; }
    public Guid? NoteId { get; set; }
    public string? RawText { get; set; }
    public int MaxTags { get; set; } = 10;
}