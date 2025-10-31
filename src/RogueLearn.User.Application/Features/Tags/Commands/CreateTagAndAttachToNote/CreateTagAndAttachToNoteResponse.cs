using RogueLearn.User.Application.Features.Tags.DTOs;

namespace RogueLearn.User.Application.Features.Tags.Commands.CreateTagAndAttachToNote;

public sealed class CreateTagAndAttachToNoteResponse
{
  public Guid NoteId { get; set; }
  public TagDto Tag { get; set; } = new();
  public bool CreatedNewTag { get; set; }
}