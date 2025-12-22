using MediatR;

namespace RogueLearn.User.Application.Features.UserProfiles.Commands.LogNewUser;

public class LogNewUserCommand : IRequest
{
	public Guid AuthUserId { get; set; }
	public string? Email { get; set; }
	public string? Username { get; set; }
}