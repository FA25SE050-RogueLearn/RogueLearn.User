using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;

public class GetMyNotesHandler : IRequestHandler<GetMyNotesQuery, List<NoteDto>>
{
  private readonly INoteRepository _noteRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly IMapper _mapper;
  private readonly ILogger<GetMyNotesHandler> _logger;

  public GetMyNotesHandler(
    INoteRepository noteRepository,
    INoteTagRepository noteTagRepository,
    IMapper mapper,
    ILogger<GetMyNotesHandler> logger)
  {
    _noteRepository = noteRepository;
    _noteTagRepository = noteTagRepository;
    _mapper = mapper;
    _logger = logger;
  }

  /// <summary>
  /// Retrieves notes for a specific authenticated user, optionally filtered by a search term, and enriches each with tag, skill, and quest IDs.
  /// </summary>
  /// <param name="request">The request containing the AuthUserId and optional search term.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A list of NoteDto for the user.</returns>
  public async Task<List<NoteDto>> Handle(GetMyNotesQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Fetching notes for auth user {AuthUserId}. SearchProvided={SearchProvided}", request.AuthUserId, !string.IsNullOrWhiteSpace(request.Search));

    var notes = string.IsNullOrWhiteSpace(request.Search)
      ? await _noteRepository.GetByUserAsync(request.AuthUserId, cancellationToken)
      : await _noteRepository.SearchByUserAsync(request.AuthUserId, request.Search!, cancellationToken);

    var list = (notes?.ToList()) ?? new List<RogueLearn.User.Domain.Entities.Note>();
    _logger.LogInformation("Found {NoteCount} notes for auth user {AuthUserId}", list.Count, request.AuthUserId);

    var dtoList = _mapper.Map<List<NoteDto>>(list);

    // Enrich with linked IDs
    for (int i = 0; i < dtoList.Count; i++)
    {
      var dto = dtoList[i];
      dto.TagIds = (await _noteTagRepository.GetTagIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
    }

    _logger.LogInformation("Returning {NoteCount} notes for auth user {AuthUserId} after enrichment", dtoList.Count, request.AuthUserId);
    return dtoList;
  }
}