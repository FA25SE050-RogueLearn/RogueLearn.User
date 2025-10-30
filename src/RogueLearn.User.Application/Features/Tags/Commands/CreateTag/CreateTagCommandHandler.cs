using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.Tags.DTOs;
using BuildingBlocks.Shared.Extensions;

namespace RogueLearn.User.Application.Features.Tags.Commands.CreateTag;

/// <summary>
/// Creates a new tag for the authenticated user. Enforces uniqueness by slug within a user's namespace.
/// </summary>
public sealed class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, CreateTagResponse>
{
  private readonly ITagRepository _tagRepository;
  private readonly ILogger<CreateTagCommandHandler> _logger;

  public CreateTagCommandHandler(ITagRepository tagRepository, ILogger<CreateTagCommandHandler> logger)
  {
    _tagRepository = tagRepository;
    _logger = logger;
  }

  public async Task<CreateTagResponse> Handle(CreateTagCommand request, CancellationToken cancellationToken)
  {
    var name = (request.Name ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Tag name is required.");
    }

    var desiredSlug = name.ToSlug();

    // Load existing user's tags and enforce uniqueness by slug
    var existing = (await _tagRepository.FindAsync(t => t.AuthUserId == request.AuthUserId, cancellationToken)).ToList();
    var conflict = existing.Any(t => (t.Name ?? string.Empty).ToSlug() == desiredSlug);
    if (conflict)
    {
      _logger.LogWarning("Tag already exists for user. AuthUserId={AuthUserId}, Name={Name}", request.AuthUserId, name);
      // Return the already existing tag instead of erroring for idempotency
      var match = existing.First(t => (t.Name ?? string.Empty).ToSlug() == desiredSlug);
      return new CreateTagResponse { Tag = new TagDto { Id = match.Id, Name = match.Name } };
    }

    var tag = new Tag
    {
      AuthUserId = request.AuthUserId,
      Name = name
    };

    var created = await _tagRepository.AddAsync(tag, cancellationToken);
    _logger.LogInformation("Created tag: TagId={TagId}, AuthUserId={AuthUserId}, Name={Name}", created.Id, created.AuthUserId, created.Name);

    return new CreateTagResponse
    {
      Tag = new TagDto { Id = created.Id, Name = created.Name }
    };
  }
}