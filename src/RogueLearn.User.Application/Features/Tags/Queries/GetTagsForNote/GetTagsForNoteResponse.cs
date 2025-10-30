using RogueLearn.User.Application.Features.Tags.DTOs;

namespace RogueLearn.User.Application.Features.Tags.Queries.GetTagsForNote;

public sealed class GetTagsForNoteResponse
{
  public Guid NoteId { get; set; }
  public List<TagDto> Tags { get; set; } = new();
}