using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.Tags.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Tags.Queries.GetTagsForNote;

/// <summary>
/// Retrieves tags associated with a note after validating ownership.
/// </summary>
public sealed class GetTagsForNoteQueryHandler : IRequestHandler<GetTagsForNoteQuery, GetTagsForNoteResponse>
{
  private readonly INoteRepository _noteRepository;
  private readonly ITagRepository _tagRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly ILogger<GetTagsForNoteQueryHandler> _logger;

  public GetTagsForNoteQueryHandler(
    INoteRepository noteRepository,
    ITagRepository tagRepository,
    INoteTagRepository noteTagRepository,
    ILogger<GetTagsForNoteQueryHandler> logger)
  {
    _noteRepository = noteRepository;
    _tagRepository = tagRepository;
    _noteTagRepository = noteTagRepository;
    _logger = logger;
  }

  public async Task<GetTagsForNoteResponse> Handle(GetTagsForNoteQuery request, CancellationToken cancellationToken)
  {
    var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);
    if (note is null || note.AuthUserId != request.AuthUserId)
    {
      _logger.LogWarning("GetTagsForNote denied: note not found or not owned. NoteId={NoteId}, AuthUserId={AuthUserId}", request.NoteId, request.AuthUserId);
      throw new InvalidOperationException("Note not found or access denied.");
    }

    var tagIds = (await _noteTagRepository.GetTagIdsForNoteAsync(note.Id, cancellationToken)).ToList();
    var tags = (await _tagRepository.FindAsync(t => tagIds.Contains(t.Id), cancellationToken)).ToList();
    var dtos = tags.Select(t => new TagDto { Id = t.Id, Name = t.Name }).ToList();

    return new GetTagsForNoteResponse { NoteId = note.Id, Tags = dtos };
  }
}