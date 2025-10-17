using MediatR;

namespace RogueLearn.User.Application.Features.Notes.Queries.GetPublicNotes;

using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;

public class GetPublicNotesQuery : IRequest<List<NoteDto>>
{
  public string? Search { get; }

  public GetPublicNotesQuery(string? search = null)
  {
    Search = search;
  }
}