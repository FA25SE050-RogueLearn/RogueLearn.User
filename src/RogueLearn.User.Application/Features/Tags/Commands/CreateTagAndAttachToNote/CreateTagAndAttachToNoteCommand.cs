using MediatR;
using RogueLearn.User.Application.Features.Tags.DTOs;

namespace RogueLearn.User.Application.Features.Tags.Commands.CreateTagAndAttachToNote;

public sealed class CreateTagAndAttachToNoteCommand : IRequest<CreateTagAndAttachToNoteResponse>
{
  public Guid AuthUserId { get; set; }
  public Guid NoteId { get; set; }
  public string Name { get; set; } = string.Empty;
}