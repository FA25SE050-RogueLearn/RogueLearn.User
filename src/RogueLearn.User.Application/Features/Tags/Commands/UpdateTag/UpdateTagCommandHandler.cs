using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Tags.DTOs;
using RogueLearn.User.Domain.Interfaces;
using BuildingBlocks.Shared.Extensions;

namespace RogueLearn.User.Application.Features.Tags.Commands.UpdateTag;

public sealed class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, UpdateTagResponse>
{
  private readonly ITagRepository _tagRepository;
  private readonly ILogger<UpdateTagCommandHandler> _logger;

  public UpdateTagCommandHandler(ITagRepository tagRepository, ILogger<UpdateTagCommandHandler> logger)
  {
    _tagRepository = tagRepository;
    _logger = logger;
  }

  public async Task<UpdateTagResponse> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
  {
    var tag = await _tagRepository.GetByIdAsync(request.TagId, cancellationToken);
    if (tag is null)
      throw new NotFoundException("Tag", request.TagId.ToString());
    if (tag.AuthUserId != request.AuthUserId)
      throw new ForbiddenException("Access denied to tag.");

    var name = (request.Name ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(name))
      throw new FluentValidation.ValidationException("Tag name is required.");

    var desiredSlug = name.ToSlug();
    var userTags = (await _tagRepository.FindAsync(t => t.AuthUserId == request.AuthUserId, cancellationToken)).ToList();
    var conflict = userTags.Any(t => t.Id != tag.Id && (t.Name ?? string.Empty).ToSlug() == desiredSlug);
    if (conflict)
      throw new ConflictException($"Tag with name '{name}' already exists.");

    if (!string.Equals(tag.Name?.Trim(), name, StringComparison.Ordinal))
    {
      tag.Name = name;
      tag = await _tagRepository.UpdateAsync(tag, cancellationToken);
      _logger.LogInformation("Updated tag: TagId={TagId}, AuthUserId={AuthUserId}, Name={Name}", tag.Id, tag.AuthUserId, tag.Name);
    }

    return new UpdateTagResponse
    {
      Tag = new TagDto { Id = tag.Id, Name = tag.Name! }
    };
  }
}