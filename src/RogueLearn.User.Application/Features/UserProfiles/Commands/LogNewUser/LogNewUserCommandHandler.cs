using MediatR;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.UserProfiles.Commands.LogNewUser;

public class LogNewUserCommandHandler : IRequestHandler<LogNewUserCommand>
{
	private readonly ILogger<LogNewUserCommandHandler> _logger;

	public LogNewUserCommandHandler(ILogger<LogNewUserCommandHandler> logger)
	{
		_logger = logger;
	}

	public Task Handle(LogNewUserCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("New user profile created via DB trigger. AuthUserId: {AuthUserId}, Email: {Email}, Username: {Username}",
			request.AuthUserId, request.Email, request.Username);

		// This handler can be expanded later to send welcome emails,
		// publish integration events, etc.

		return Task.CompletedTask;
	}
}