using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;

public class UpdateNoteHandler : IRequestHandler<UpdateNoteCommand, UpdateNoteResponse>
{
  private readonly INoteRepository _noteRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly INoteSkillRepository _noteSkillRepository;
  private readonly INoteQuestRepository _noteQuestRepository;
  private readonly IMapper _mapper;

  public UpdateNoteHandler(
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

  public async Task<UpdateNoteResponse> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
  {
    var note = await _noteRepository.GetByIdAsync(request.Id, cancellationToken);
    if (note is null)
    {
      throw new NotFoundException($"Note with ID {request.Id} not found.");
    }

    if (note.AuthUserId != request.AuthUserId)
    {
      throw new ForbiddenException("You are not allowed to update this note.");
    }

    note.Title = request.Title;
    note.Content = request.Content;
    note.IsPublic = request.IsPublic;
    note.UpdatedAt = DateTimeOffset.UtcNow;

    var updated = await _noteRepository.UpdateAsync(note, cancellationToken);

    // Apply relationship updates if provided
    if (request.TagIds is not null)
    {
      var existing = (await _noteTagRepository.GetTagIdsForNoteAsync(note.Id, cancellationToken)).ToHashSet();
      var desired = request.TagIds.Distinct().ToHashSet();

      var toAdd = desired.Except(existing);
      var toRemove = existing.Except(desired);

      foreach (var tagId in toAdd)
        await _noteTagRepository.AddAsync(note.Id, tagId, cancellationToken);

      foreach (var tagId in toRemove)
        await _noteTagRepository.RemoveAsync(note.Id, tagId, cancellationToken);
    }

    if (request.SkillIds is not null)
    {
      var existing = (await _noteSkillRepository.GetSkillIdsForNoteAsync(note.Id, cancellationToken)).ToHashSet();
      var desired = request.SkillIds.Distinct().ToHashSet();

      var toAdd = desired.Except(existing);
      var toRemove = existing.Except(desired);

      foreach (var skillId in toAdd)
        await _noteSkillRepository.AddAsync(note.Id, skillId, cancellationToken);

      foreach (var skillId in toRemove)
        await _noteSkillRepository.RemoveAsync(note.Id, skillId, cancellationToken);
    }

    if (request.QuestIds is not null)
    {
      var existing = (await _noteQuestRepository.GetQuestIdsForNoteAsync(note.Id, cancellationToken)).ToHashSet();
      var desired = request.QuestIds.Distinct().ToHashSet();

      var toAdd = desired.Except(existing);
      var toRemove = existing.Except(desired);

      foreach (var questId in toAdd)
        await _noteQuestRepository.AddAsync(note.Id, questId, cancellationToken);

      foreach (var questId in toRemove)
        await _noteQuestRepository.RemoveAsync(note.Id, questId, cancellationToken);
    }

    return _mapper.Map<UpdateNoteResponse>(updated);
  }
}