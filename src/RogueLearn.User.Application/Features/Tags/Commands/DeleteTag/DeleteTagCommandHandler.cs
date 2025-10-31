using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Tags.Commands.DeleteTag;

/// <summary>
/// Deletes a tag owned by the authenticated user and detaches it from all notes.
/// </summary>
public sealed class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand>
{
  private readonly ITagRepository _tagRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly ILogger<DeleteTagCommandHandler> _logger;

  public DeleteTagCommandHandler(ITagRepository tagRepository, INoteTagRepository noteTagRepository, ILogger<DeleteTagCommandHandler> logger)
  {
    _tagRepository = tagRepository;
    _noteTagRepository = noteTagRepository;
    _logger = logger;
  }

  public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken)
  {
    var tag = await _tagRepository.GetByIdAsync(request.TagId, cancellationToken);
    if (tag is null || tag.AuthUserId != request.AuthUserId)
    {
      _logger.LogWarning("DeleteTag denied or not found. TagId={TagId}, AuthUserId={AuthUserId}", request.TagId, request.AuthUserId);
      return; // Idempotent no-op
    }

    var noteIds = await _noteTagRepository.GetNoteIdsByTagIdAsync(tag.Id, cancellationToken);
    foreach (var noteId in noteIds)
    {
      await _noteTagRepository.RemoveAsync(noteId, tag.Id, cancellationToken);
    }

    await _tagRepository.DeleteAsync(tag.Id, cancellationToken);
    _logger.LogInformation("Deleted tag TagId={TagId} for AuthUserId={AuthUserId}", tag.Id, request.AuthUserId);
    return;
  }
}