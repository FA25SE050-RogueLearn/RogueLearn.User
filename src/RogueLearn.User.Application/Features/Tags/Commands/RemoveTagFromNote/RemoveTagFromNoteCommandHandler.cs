using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Tags.Commands.RemoveTagFromNote;

/// <summary>
/// Detaches a tag from a user's note after validating ownership.
/// </summary>
public sealed class RemoveTagFromNoteCommandHandler : IRequestHandler<RemoveTagFromNoteCommand>
{
  private readonly INoteRepository _noteRepository;
  private readonly ITagRepository _tagRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly ILogger<RemoveTagFromNoteCommandHandler> _logger;

  public RemoveTagFromNoteCommandHandler(
    INoteRepository noteRepository,
    ITagRepository tagRepository,
    INoteTagRepository noteTagRepository,
    ILogger<RemoveTagFromNoteCommandHandler> logger)
  {
    _noteRepository = noteRepository;
    _tagRepository = tagRepository;
    _noteTagRepository = noteTagRepository;
    _logger = logger;
  }

  public async Task Handle(RemoveTagFromNoteCommand request, CancellationToken cancellationToken)
  {
    var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);
    if (note is null || note.AuthUserId != request.AuthUserId)
    {
      _logger.LogWarning("RemoveTag denied: note not found or not owned. NoteId={NoteId}, AuthUserId={AuthUserId}", request.NoteId, request.AuthUserId);
      return;
    }

    var tag = await _tagRepository.GetByIdAsync(request.TagId, cancellationToken);
    if (tag is null || tag.AuthUserId != request.AuthUserId)
    {
      _logger.LogWarning("RemoveTag denied: tag not found or not owned. TagId={TagId}, AuthUserId={AuthUserId}", request.TagId, request.AuthUserId);
      return;
    }

    await _noteTagRepository.RemoveAsync(note.Id, tag.Id, cancellationToken);
    _logger.LogInformation("Removed tag from note. NoteId={NoteId}, TagId={TagId}", note.Id, tag.Id);
    return;
  }
}