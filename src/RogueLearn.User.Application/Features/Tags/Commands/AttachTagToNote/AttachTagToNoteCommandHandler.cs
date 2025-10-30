using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.Tags.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Tags.Commands.AttachTagToNote;

/// <summary>
/// Attaches an existing tag to a user's note. Validates ownership of both note and tag.
/// </summary>
public sealed class AttachTagToNoteCommandHandler : IRequestHandler<AttachTagToNoteCommand, AttachTagToNoteResponse>
{
  private readonly INoteRepository _noteRepository;
  private readonly ITagRepository _tagRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly ILogger<AttachTagToNoteCommandHandler> _logger;

  public AttachTagToNoteCommandHandler(
    INoteRepository noteRepository,
    ITagRepository tagRepository,
    INoteTagRepository noteTagRepository,
    ILogger<AttachTagToNoteCommandHandler> logger)
  {
    _noteRepository = noteRepository;
    _tagRepository = tagRepository;
    _noteTagRepository = noteTagRepository;
    _logger = logger;
  }

  public async Task<AttachTagToNoteResponse> Handle(AttachTagToNoteCommand request, CancellationToken cancellationToken)
  {
    var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);
    if (note is null || note.AuthUserId != request.AuthUserId)
    {
      _logger.LogWarning("AttachTag denied: note not found or not owned. NoteId={NoteId}, AuthUserId={AuthUserId}", request.NoteId, request.AuthUserId);
      throw new InvalidOperationException("Note not found or access denied.");
    }

    var tag = await _tagRepository.GetByIdAsync(request.TagId, cancellationToken);
    if (tag is null || tag.AuthUserId != request.AuthUserId)
    {
      _logger.LogWarning("AttachTag denied: tag not found or not owned. TagId={TagId}, AuthUserId={AuthUserId}", request.TagId, request.AuthUserId);
      throw new InvalidOperationException("Tag not found or access denied.");
    }

    var currentTagIds = (await _noteTagRepository.GetTagIdsForNoteAsync(note.Id, cancellationToken)).ToHashSet();
    var already = currentTagIds.Contains(tag.Id);
    if (!already)
    {
      await _noteTagRepository.AddAsync(note.Id, tag.Id, cancellationToken);
    }

    return new AttachTagToNoteResponse
    {
      NoteId = note.Id,
      Tag = new TagDto { Id = tag.Id, Name = tag.Name },
      AlreadyAttached = already
    };
  }
}