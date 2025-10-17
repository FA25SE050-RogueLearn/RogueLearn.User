using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;

public class GetMyNotesHandler : IRequestHandler<GetMyNotesQuery, List<NoteDto>>
{
  private readonly INoteRepository _noteRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly INoteSkillRepository _noteSkillRepository;
  private readonly INoteQuestRepository _noteQuestRepository;
  private readonly IMapper _mapper;

  public GetMyNotesHandler(
    INoteRepository noteRepository,
    INoteTagRepository noteTagRepository,
    INoteSkillRepository noteSkillRepository,
    INoteQuestRepository noteQuestRepository,
    IMapper mapper)
  {
    _noteRepository = noteRepository;
    _noteTagRepository = noteTagRepository;
    _noteSkillRepository = noteSkillRepository;
    _noteQuestRepository = noteQuestRepository;
    _mapper = mapper;
  }

  public async Task<List<NoteDto>> Handle(GetMyNotesQuery request, CancellationToken cancellationToken)
  {
    var notes = string.IsNullOrWhiteSpace(request.Search)
      ? await _noteRepository.GetByUserAsync(request.AuthUserId, cancellationToken)
      : await _noteRepository.SearchByUserAsync(request.AuthUserId, request.Search!, cancellationToken);

    var dtoList = _mapper.Map<List<NoteDto>>(notes);

    // Enrich with linked IDs
    for (int i = 0; i < dtoList.Count; i++)
    {
      var dto = dtoList[i];
      dto.TagIds = (await _noteTagRepository.GetTagIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
      dto.SkillIds = (await _noteSkillRepository.GetSkillIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
      dto.QuestIds = (await _noteQuestRepository.GetQuestIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
    }

    return dtoList;
  }
}