using MediatR;
using RogueLearn.User.Application.Features.Tags.DTOs;

namespace RogueLearn.User.Application.Features.Tags.Commands.CreateTag;

public sealed class CreateTagCommand : IRequest<CreateTagResponse>
{
  public Guid AuthUserId { get; set; }
  public string Name { get; set; } = string.Empty;
}