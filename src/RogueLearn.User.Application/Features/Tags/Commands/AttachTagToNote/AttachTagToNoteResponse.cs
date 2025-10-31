using RogueLearn.User.Application.Features.Tags.DTOs;

namespace RogueLearn.User.Application.Features.Tags.Commands.AttachTagToNote;

public sealed class AttachTagToNoteResponse
{
  public Guid NoteId { get; set; }
  public TagDto Tag { get; set; } = new();
  public bool AlreadyAttached { get; set; }
}