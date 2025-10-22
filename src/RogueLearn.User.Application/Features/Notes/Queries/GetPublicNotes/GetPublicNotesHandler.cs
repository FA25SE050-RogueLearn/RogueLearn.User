using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Notes.Queries.GetPublicNotes;

public class GetPublicNotesHandler : IRequestHandler<GetPublicNotesQuery, List<NoteDto>>
{
  private readonly INoteRepository _noteRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly INoteSkillRepository _noteSkillRepository;
  private readonly INoteQuestRepository _noteQuestRepository;
  private readonly IMapper _mapper;
  private readonly ILogger<GetPublicNotesHandler> _logger;

  public GetPublicNotesHandler(
    INoteRepository noteRepository,
    INoteTagRepository noteTagRepository,
    INoteSkillRepository noteSkillRepository,
    INoteQuestRepository noteQuestRepository,
    IMapper mapper,
    ILogger<GetPublicNotesHandler> logger)
  {
    _noteRepository = noteRepository;
    _noteTagRepository = noteTagRepository;
    _noteSkillRepository = noteSkillRepository;
    _noteQuestRepository = noteQuestRepository;
    _mapper = mapper;
    _logger = logger;
  }

  /// <summary>
  /// Retrieves public notes, optionally filtered by a search term, and enriches each with tag, skill, and quest IDs.
  /// </summary>
  /// <param name="request">The request containing an optional search term.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A list of NoteDto representing public notes.</returns>
  public async Task<List<NoteDto>> Handle(GetPublicNotesQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Fetching public notes. SearchProvided={SearchProvided}", !string.IsNullOrWhiteSpace(request.Search));

    var notes = string.IsNullOrWhiteSpace(request.Search)
      ? await _noteRepository.GetPublicAsync(cancellationToken)
      : await _noteRepository.SearchPublicAsync(request.Search!, cancellationToken);

    var list = (notes?.ToList()) ?? new List<RogueLearn.User.Domain.Entities.Note>();
    _logger.LogInformation("Found {NoteCount} public notes", list.Count);

    var dtoList = _mapper.Map<List<NoteDto>>(list);

    for (int i = 0; i < dtoList.Count; i++)
    {
      var dto = dtoList[i];
      dto.TagIds = (await _noteTagRepository.GetTagIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
      dto.SkillIds = (await _noteSkillRepository.GetSkillIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
      dto.QuestIds = (await _noteQuestRepository.GetQuestIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
    }

    _logger.LogInformation("Returning {NoteCount} public notes after enrichment", dtoList.Count);
    return dtoList;
  }
}