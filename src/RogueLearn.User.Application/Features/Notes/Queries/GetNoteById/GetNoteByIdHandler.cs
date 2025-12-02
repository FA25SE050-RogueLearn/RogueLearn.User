using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Notes.Queries.GetNoteById;

public class GetNoteByIdHandler : IRequestHandler<GetNoteByIdQuery, NoteDto?>
{
  private readonly INoteRepository _noteRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly IMapper _mapper;
  private readonly ILogger<GetNoteByIdHandler> _logger;

  public GetNoteByIdHandler(
    INoteRepository noteRepository,
    INoteTagRepository noteTagRepository,
    IMapper mapper,
    ILogger<GetNoteByIdHandler> logger)
  {
    _noteRepository = noteRepository;
    _noteTagRepository = noteTagRepository;
    _mapper = mapper;
    _logger = logger;
  }

  /// <summary>
  /// Retrieves a single note by its ID and enriches it with linked tag, skill, and quest IDs.
  /// </summary>
  /// <param name="request">The request containing the note Id.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The NoteDto if found; otherwise null.</returns>
  public async Task<NoteDto?> Handle(GetNoteByIdQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Fetching note by id {NoteId}", request.Id);
    var note = await _noteRepository.GetByIdAsync(request.Id, cancellationToken);
    if (note is null)
    {
      _logger.LogInformation("No note found with id {NoteId}", request.Id);
      return null;
    }

    var dto = _mapper.Map<NoteDto>(note);
    dto.TagIds = (await _noteTagRepository.GetTagIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
    _logger.LogInformation("Returning note {NoteId} with {TagCount} tags", dto.Id, dto.TagIds.Count);
    return dto;
  }
}