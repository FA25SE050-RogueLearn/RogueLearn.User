using MediatR;

namespace RogueLearn.User.Application.Features.Tags.Commands.UpdateTag;

public sealed class UpdateTagCommand : IRequest<UpdateTagResponse>
{
  public Guid AuthUserId { get; set; }
  public Guid TagId { get; set; }
  public string Name { get; set; } = string.Empty;
}