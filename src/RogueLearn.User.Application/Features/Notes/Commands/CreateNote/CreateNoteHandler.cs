using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notes.Commands.CreateNote;

/// <summary>
/// Handles creation of a new Note for the authenticated user.
/// - Validates via pipeline validators.
/// - Creates the Note and sets audit fields in the handler (not via mappers).
/// - Optionally assigns tags, skills, and quests in a deterministic, idempotent manner.
/// - Returns a typed response DTO.
/// </summary>
public class CreateNoteHandler : IRequestHandler<CreateNoteCommand, CreateNoteResponse>
{
  private readonly INoteRepository _noteRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly INoteSkillRepository _noteSkillRepository;
  private readonly INoteQuestRepository _noteQuestRepository;
  private readonly IMapper _mapper;
  private readonly ILogger<CreateNoteHandler> _logger;

  public CreateNoteHandler(
    INoteRepository noteRepository,
    INoteTagRepository noteTagRepository,
    INoteSkillRepository noteSkillRepository,
    INoteQuestRepository noteQuestRepository,
    IMapper mapper,
    ILogger<CreateNoteHandler> logger)
  {
    _noteRepository = noteRepository;
    _noteTagRepository = noteTagRepository;
    _noteSkillRepository = noteSkillRepository;
    _noteQuestRepository = noteQuestRepository;
    _mapper = mapper;
    _logger = logger;
  }

  /// <summary>
  /// Creates a note and applies optional relationship assignments.
  /// Side-effects (storage) occur after validations and are grouped for consistency.
  /// </summary>
  public async Task<CreateNoteResponse> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Starting note creation for AuthUserId {AuthUserId}", request.AuthUserId);

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

    _logger.LogInformation("Completed note creation. NoteId={NoteId}", created.Id);

    return _mapper.Map<CreateNoteResponse>(created);
  }
}