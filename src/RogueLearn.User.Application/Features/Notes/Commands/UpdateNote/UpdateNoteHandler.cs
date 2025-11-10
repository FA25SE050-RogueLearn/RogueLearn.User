using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;

/// <summary>
/// Handles updating an existing Note for the authenticated user.
/// - Loads the Note, enforces ownership (authorization) and domain invariants.
/// - Applies updates and related assignments deterministically.
/// - Throws standardized exceptions for expected error conditions.
/// </summary>
public class UpdateNoteHandler : IRequestHandler<UpdateNoteCommand, UpdateNoteResponse>
{
  private readonly INoteRepository _noteRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly INoteSkillRepository _noteSkillRepository;
  private readonly INoteQuestRepository _noteQuestRepository;
  private readonly IMapper _mapper;
  private readonly ILogger<UpdateNoteHandler> _logger;

  public UpdateNoteHandler(
    INoteRepository noteRepository,
    INoteTagRepository noteTagRepository,
    INoteSkillRepository noteSkillRepository,
    INoteQuestRepository noteQuestRepository,
    IMapper mapper,
    ILogger<UpdateNoteHandler> logger)
  {
    _noteRepository = noteRepository;
    _noteTagRepository = noteTagRepository;
    _noteSkillRepository = noteSkillRepository;
    _noteQuestRepository = noteQuestRepository;
    _mapper = mapper;
    _logger = logger;
  }

  /// <summary>
  /// Updates Note fields and synchronizes relationships.
  /// </summary>
  public async Task<UpdateNoteResponse> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Starting note update {NoteId} for AuthUserId {AuthUserId}", request.Id, request.AuthUserId);

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
    // Normalize incoming content to BlockNote's top-level array (List<object>)
    var normalizedContent = ConvertToBlockNoteArray(request.Content);
    _logger.LogInformation(
      "[UpdateNote] Outgoing Content type: {Type}; Preview: {Preview}",
      normalizedContent?.GetType().Name ?? "null",
      normalizedContent is null ? "<null>" : JsonSerializer.Serialize(normalizedContent));
    note.Content = normalizedContent;
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

    _logger.LogInformation("Completed note update. NoteId={NoteId}", updated.Id);

    return _mapper.Map<UpdateNoteResponse>(updated);
  }

  private static List<object>? ConvertToBlockNoteArray(object? content)
  {
    if (content is null)
      return null;

    if (content is List<object> lo)
      return lo;

    if (content is JsonElement je)
    {
      var converted = ConvertJsonElement(je);
      return converted as List<object> ?? (converted is null ? null : new List<object> { converted });
    }

    if (content is string s)
    {
      if (string.IsNullOrWhiteSpace(s))
        return null;
      try
      {
        var element = JsonSerializer.Deserialize<JsonElement>(s, (JsonSerializerOptions?)null);
        var converted = ConvertJsonElement(element);
        return converted as List<object> ?? (converted is null ? null : new List<object> { converted });
      }
      catch
      {
        // Treat as plain text; wrap into BlockNote paragraph structure minimal array
        return new List<object>
        {
          new Dictionary<string, object?>
          {
            { "type", "paragraph" },
            { "content", new List<object>
              {
                new Dictionary<string, object?>
                {
                  { "type", "text" },
                  { "text", s }
                }
              }
            }
          }
        };
      }
    }

    // For dictionaries or other objects, wrap into an array to satisfy BlockNote's top-level structure
    return new List<object> { content };
  }

  private static object? ConvertJsonElement(JsonElement element)
  {
    return element.ValueKind switch
    {
      JsonValueKind.Object => element.EnumerateObject()
        .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
      JsonValueKind.Array => element.EnumerateArray()
        .Select(ConvertJsonElement)
        .ToList(),
      JsonValueKind.String => element.GetString(),
      JsonValueKind.Number => element.TryGetInt32(out var intValue)
        ? (object)intValue
        : element.GetDouble(),
      JsonValueKind.True => true,
      JsonValueKind.False => false,
      JsonValueKind.Null => null,
      _ => element.ToString()
    };
  }
}