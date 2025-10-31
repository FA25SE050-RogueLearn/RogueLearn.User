using MediatR;

namespace RogueLearn.User.Application.Features.Tags.Commands.RemoveTagFromNote;

public sealed class RemoveTagFromNoteCommand : IRequest
{
  public Guid AuthUserId { get; set; }
  public Guid NoteId { get; set; }
  public Guid TagId { get; set; }
}