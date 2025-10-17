using MediatR;

namespace RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;

public class GetMyNotesQuery : IRequest<List<NoteDto>>
{
  public Guid AuthUserId { get; }
  public string? Search { get; }

  public GetMyNotesQuery(Guid authUserId, string? search = null)
  {
    AuthUserId = authUserId;
    Search = search;
  }
}