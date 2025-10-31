using MediatR;

namespace RogueLearn.User.Application.Features.Tags.Queries.GetTagsForNote;

public sealed class GetTagsForNoteQuery : IRequest<GetTagsForNoteResponse>
{
  public Guid AuthUserId { get; set; }
  public Guid NoteId { get; set; }
}