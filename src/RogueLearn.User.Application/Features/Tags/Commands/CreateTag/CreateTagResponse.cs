using RogueLearn.User.Application.Features.Tags.DTOs;

namespace RogueLearn.User.Application.Features.Tags.Commands.CreateTag;

public sealed class CreateTagResponse
{
  public TagDto Tag { get; set; } = new();
}