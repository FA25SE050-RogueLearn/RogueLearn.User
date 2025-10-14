using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Features.UserProfiles.Commands.LogNewUser;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.UserProfiles.Commands.LogNewUser;

public class LogNewUserCommandHandlerTests
{
	private readonly Mock<ILogger<LogNewUserCommandHandler>> _mockLogger;
	private readonly LogNewUserCommandHandler _handler;

	public LogNewUserCommandHandlerTests()
	{
		_mockLogger = new Mock<ILogger<LogNewUserCommandHandler>>();
		_handler = new LogNewUserCommandHandler(_mockLogger.Object);
	}

	[Fact]
	public async Task Handle_ShouldLogInformation()
	{
		// Arrange
		var command = new LogNewUserCommand
		{
			AuthUserId = Guid.NewGuid(),
			Email = "test@example.com",
			Username = "testuser"
		};

		// Act
		await _handler.Handle(command, CancellationToken.None);

		// Assert
		// This verifies that the LogInformation method was called exactly once.
		// It uses Moq's It.IsAny<string>() to match any log message format,
		// which is a robust way to test logging.
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => true),
				null,
				It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
			Times.Once);
	}
}