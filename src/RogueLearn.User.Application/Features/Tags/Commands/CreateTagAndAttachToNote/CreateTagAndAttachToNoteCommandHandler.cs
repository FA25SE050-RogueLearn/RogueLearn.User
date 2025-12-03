using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.Tags.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using BuildingBlocks.Shared.Extensions;

namespace RogueLearn.User.Application.Features.Tags.Commands.CreateTagAndAttachToNote;

/// <summary>
/// Creates (if necessary) a tag within the user's namespace and attaches it to the given note.
/// </summary>
public sealed class CreateTagAndAttachToNoteCommandHandler : IRequestHandler<CreateTagAndAttachToNoteCommand, CreateTagAndAttachToNoteResponse>
{
  private readonly INoteRepository _noteRepository;
  private readonly ITagRepository _tagRepository;
  private readonly INoteTagRepository _noteTagRepository;
  private readonly ILogger<CreateTagAndAttachToNoteCommandHandler> _logger;

  public CreateTagAndAttachToNoteCommandHandler(
    INoteRepository noteRepository,
    ITagRepository tagRepository,
    INoteTagRepository noteTagRepository,
    ILogger<CreateTagAndAttachToNoteCommandHandler> logger)
  {
    _noteRepository = noteRepository;
    _tagRepository = tagRepository;
    _noteTagRepository = noteTagRepository;
    _logger = logger;
  }

  public async Task<CreateTagAndAttachToNoteResponse> Handle(CreateTagAndAttachToNoteCommand request, CancellationToken cancellationToken)
  {
    var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);
    if (note is null || note.AuthUserId != request.AuthUserId)
    {
      _logger.LogWarning("CreateTagAndAttach denied: note not found or not owned. NoteId={NoteId}, AuthUserId={AuthUserId}", request.NoteId, request.AuthUserId);
      throw new InvalidOperationException("Note not found or access denied.");
    }

    var name = (request.Name ?? string.Empty).Trim().ToPascalCase();
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Tag name is required.");
    }

    var slug = name.ToSlug();
    var userTags = (await _tagRepository.FindAsync(t => t.AuthUserId == request.AuthUserId, cancellationToken)).ToList();
    var tag = userTags.FirstOrDefault(t => (t.Name ?? string.Empty).ToSlug() == slug);

    var createdNew = false;
    if (tag is null)
    {
      tag = new Tag
      {
        AuthUserId = request.AuthUserId,
        Name = name
      };
      tag = await _tagRepository.AddAsync(tag, cancellationToken);
      createdNew = true;
      _logger.LogInformation("Created new tag and will attach. TagId={TagId}, Name={Name}", tag.Id, tag.Name);
    }

    var currentTagIds = (await _noteTagRepository.GetTagIdsForNoteAsync(note.Id, cancellationToken)).ToHashSet();
    if (!currentTagIds.Contains(tag.Id))
    {
      await _noteTagRepository.AddAsync(note.Id, tag.Id, cancellationToken);
    }

    return new CreateTagAndAttachToNoteResponse
    {
      NoteId = note.Id,
      Tag = new TagDto { Id = tag.Id, Name = tag.Name },
      CreatedNewTag = createdNew
    };
  }
}