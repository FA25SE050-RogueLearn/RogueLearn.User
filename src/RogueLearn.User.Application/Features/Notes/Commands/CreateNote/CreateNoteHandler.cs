using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

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
    // Normalize incoming content to BlockNote's top-level array (List<object>)
    var normalizedContent = ConvertToBlockNoteArray(request.Content);
    _logger.LogInformation(
      "[CreateNote] Outgoing Content type: {Type}; Preview: {Preview}",
      normalizedContent?.GetType().Name ?? "null",
      normalizedContent is null ? "<null>" : JsonSerializer.Serialize(normalizedContent));

    var note = new Note
    {
      Id = Guid.NewGuid(),
      AuthUserId = request.AuthUserId,
      Title = request.Title,
      Content = normalizedContent,
      IsPublic = request.IsPublic,
      CreatedAt = DateTimeOffset.UtcNow,
      UpdatedAt = DateTimeOffset.UtcNow
    };

    var created = await _noteRepository.AddAsync(note, cancellationToken);
    _logger.LogInformation("Created note: NoteId={NoteId}, Title={Title}, IsPublic={IsPublic}, StoredContentType={StoredType}", created.Id, created.Title, created.IsPublic, created.Content?.GetType().FullName ?? "null");
    try
    {
      var storedPreview = created.Content is not null ? JsonSerializer.Serialize(created.Content) : "null";
      if (storedPreview.Length > 500) storedPreview = storedPreview[..500] + "...";
      _logger.LogInformation("[Post-Insert] Stored Content preview: {Preview}", storedPreview);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to serialize Stored Content preview after insert (CreateNote)");
    }

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