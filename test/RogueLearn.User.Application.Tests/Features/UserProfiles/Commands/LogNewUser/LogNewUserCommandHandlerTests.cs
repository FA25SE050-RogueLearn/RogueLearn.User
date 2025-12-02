using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.UserProfiles.Commands.LogNewUser;

namespace RogueLearn.User.Application.Tests.Features.UserProfiles.Commands.LogNewUser;

public class LogNewUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_LogsInformation()
    {
        var logger = Substitute.For<ILogger<LogNewUserCommandHandler>>();
        var sut = new LogNewUserCommandHandler(logger);
        var cmd = new LogNewUserCommand { AuthUserId = Guid.NewGuid(), Email = "a@b.com", Username = "user" };
        await sut.Handle(cmd, CancellationToken.None);
        logger.ReceivedWithAnyArgs(1).Log<object>(default, default, default!, default!, default!);
    }
}