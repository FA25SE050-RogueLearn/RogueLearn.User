using MediatR;

namespace RogueLearn.User.Application.Features.Tags.Queries.GetMyTags;

public sealed class GetMyTagsQuery : IRequest<GetMyTagsResponse>
{
  public Guid AuthUserId { get; }
  public string? Search { get; }

  public GetMyTagsQuery(Guid authUserId, string? search = null)
  {
    AuthUserId = authUserId;
    Search = search;
  }
}