using MediatR;
using RogueLearn.User.Application.Features.Tags.DTOs;

namespace RogueLearn.User.Application.Features.Tags.Commands.AttachTagToNote;

public sealed class AttachTagToNoteCommand : IRequest<AttachTagToNoteResponse>
{
  public Guid AuthUserId { get; set; }
  public Guid NoteId { get; set; }
  public Guid TagId { get; set; }
}