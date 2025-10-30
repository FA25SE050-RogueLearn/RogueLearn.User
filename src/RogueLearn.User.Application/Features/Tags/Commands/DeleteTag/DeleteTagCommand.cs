using MediatR;

namespace RogueLearn.User.Application.Features.Tags.Commands.DeleteTag;

public sealed class DeleteTagCommand : IRequest
{
  public Guid AuthUserId { get; set; }
  public Guid TagId { get; set; }
}