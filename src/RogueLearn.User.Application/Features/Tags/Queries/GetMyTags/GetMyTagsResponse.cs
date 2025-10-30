using RogueLearn.User.Application.Features.Tags.DTOs;

namespace RogueLearn.User.Application.Features.Tags.Queries.GetMyTags;

public sealed class GetMyTagsResponse
{
  public List<TagDto> Tags { get; set; } = new();
}