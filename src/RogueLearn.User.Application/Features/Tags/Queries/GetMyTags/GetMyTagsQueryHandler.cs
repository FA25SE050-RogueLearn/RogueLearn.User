using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.Tags.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Tags.Queries.GetMyTags;

/// <summary>
/// Retrieves all tags owned by the authenticated user. Optional search by name (case-insensitive, contains).
/// </summary>
public sealed class GetMyTagsQueryHandler : IRequestHandler<GetMyTagsQuery, GetMyTagsResponse>
{
  private readonly ITagRepository _tagRepository;
  private readonly ILogger<GetMyTagsQueryHandler> _logger;

  public GetMyTagsQueryHandler(ITagRepository tagRepository, ILogger<GetMyTagsQueryHandler> logger)
  {
    _tagRepository = tagRepository;
    _logger = logger;
  }

  public async Task<GetMyTagsResponse> Handle(GetMyTagsQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Fetching tags for AuthUserId={AuthUserId}", request.AuthUserId);
    var all = (await _tagRepository.FindAsync(t => t.AuthUserId == request.AuthUserId, cancellationToken)).ToList();

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var term = request.Search.Trim();
      all = all.Where(t => (t.Name ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    var dtos = all.Select(t => new TagDto { Id = t.Id, Name = t.Name }).ToList();
    _logger.LogInformation("Returning {Count} tags for AuthUserId={AuthUserId}", dtos.Count, request.AuthUserId);
    return new GetMyTagsResponse { Tags = dtos };
  }
}