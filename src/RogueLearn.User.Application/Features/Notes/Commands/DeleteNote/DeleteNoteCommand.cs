using MediatR;

namespace RogueLearn.User.Application.Features.Notes.Commands.DeleteNote;

public class DeleteNoteCommand : IRequest
{
  public Guid Id { get; set; }
  public Guid AuthUserId { get; set; }
}