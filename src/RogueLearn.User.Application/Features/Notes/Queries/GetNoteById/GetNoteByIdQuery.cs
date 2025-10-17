using MediatR;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;

namespace RogueLearn.User.Application.Features.Notes.Queries.GetNoteById;

public class GetNoteByIdQuery : IRequest<NoteDto?>
{
  public Guid Id { get; set; }
}