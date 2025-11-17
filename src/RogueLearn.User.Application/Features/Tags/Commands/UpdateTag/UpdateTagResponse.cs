using RogueLearn.User.Application.Features.Tags.DTOs;

namespace RogueLearn.User.Application.Features.Tags.Commands.UpdateTag;

public sealed class UpdateTagResponse
{
  public TagDto Tag { get; set; } = new();
}