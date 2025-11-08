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
    if (note is null)
    {
      _logger.LogWarning("GetTagsForNote failed: note not found. NoteId={NoteId}", request.NoteId);
      throw new Exceptions.NotFoundException("Note", request.NoteId.ToString());
    }

    if (note.AuthUserId != request.AuthUserId)
    {
      _logger.LogWarning("GetTagsForNote denied: note not owned. NoteId={NoteId}, AuthUserId={AuthUserId}", request.NoteId, request.AuthUserId);
      throw new Exceptions.ForbiddenException("Access denied to note.");
    }

    var tagIds = (await _noteTagRepository.GetTagIdsForNoteAsync(note.Id, cancellationToken)).ToList();
    var tags = (await _tagRepository.GetByIdsAsync(tagIds, cancellationToken)).ToList();
    var dtos = tags.Select(t => new TagDto { Id = t.Id, Name = t.Name }).ToList();

    return new GetTagsForNoteResponse { NoteId = note.Id, Tags = dtos };
  }
}