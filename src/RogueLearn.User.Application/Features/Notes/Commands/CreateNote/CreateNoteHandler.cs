using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNote;

public class CreateNoteHandler : IRequestHandler<CreateNoteCommand, CreateNoteResponse>
{
  private readonly INoteRepository _noteRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly INoteSkillRepository _noteSkillRepository;
  private readonly INoteQuestRepository _noteQuestRepository;
  private readonly IMapper _mapper;

  public CreateNoteHandler(
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

  public async Task<CreateNoteResponse> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
  {
    var note = new Note
    {
      Id = Guid.NewGuid(),
      AuthUserId = request.AuthUserId,
      Title = request.Title,
      Content = request.Content,
      IsPublic = request.IsPublic,
      CreatedAt = DateTimeOffset.UtcNow,
      UpdatedAt = DateTimeOffset.UtcNow
    };

    var created = await _noteRepository.AddAsync(note, cancellationToken);

    if (request.TagIds is { Count: > 0 })
    {
      foreach (var tagId in request.TagIds.Distinct())
      {
        await _noteTagRepository.AddAsync(created.Id, tagId, cancellationToken);
      }
    }

    if (request.SkillIds is { Count: > 0 })
    {
      foreach (var skillId in request.SkillIds.Distinct())
      {
        await _noteSkillRepository.AddAsync(created.Id, skillId, cancellationToken);
      }
    }

    if (request.QuestIds is { Count: > 0 })
    {
      foreach (var questId in request.QuestIds.Distinct())
      {
        await _noteQuestRepository.AddAsync(created.Id, questId, cancellationToken);
      }
    }

    return _mapper.Map<CreateNoteResponse>(created);
  }
}