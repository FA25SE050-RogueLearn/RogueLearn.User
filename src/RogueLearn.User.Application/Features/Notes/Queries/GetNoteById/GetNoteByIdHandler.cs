using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notes.Queries.GetNoteById;

public class GetNoteByIdHandler : IRequestHandler<GetNoteByIdQuery, NoteDto?>
{
  private readonly INoteRepository _noteRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly INoteSkillRepository _noteSkillRepository;
  private readonly INoteQuestRepository _noteQuestRepository;
  private readonly IMapper _mapper;

  public GetNoteByIdHandler(
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

  public async Task<NoteDto?> Handle(GetNoteByIdQuery request, CancellationToken cancellationToken)
  {
    var note = await _noteRepository.GetByIdAsync(request.Id, cancellationToken);
    if (note is null)
      return null;

    var dto = _mapper.Map<NoteDto>(note);
    dto.TagIds = (await _noteTagRepository.GetTagIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
    dto.SkillIds = (await _noteSkillRepository.GetSkillIdsForNoteAsync(dto.Id, cancellationToken)).ToList();
    dto.QuestIds = (await _noteQuestRepository.GetQuestIdsForNoteAsync(dto.Id, cancellationToken)).ToList();

    return dto;
  }
}