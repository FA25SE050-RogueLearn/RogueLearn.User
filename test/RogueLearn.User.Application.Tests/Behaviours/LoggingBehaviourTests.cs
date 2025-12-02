using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Behaviours;

namespace RogueLearn.User.Application.Tests.Behaviours;

public class LoggingBehaviourTests
{
    public record DummyRequest(string Payload) : IRequest<string>;
    private static Task<string> NextOk(CancellationToken ct) => Task.FromResult("ok");

    [Fact]
    public async Task Handle_Logs_Before_And_After_And_Calls_Next()
    {
        var logger = Substitute.For<ILogger<LoggingBehaviour<DummyRequest, string>>>();
        var sut = new LoggingBehaviour<DummyRequest, string>(logger);
        var request = new DummyRequest("data");
        var res = await sut.Handle(request, new RequestHandlerDelegate<string>(NextOk), CancellationToken.None);
        Assert.Equal("ok", res);
        logger.ReceivedWithAnyArgs(2).Log<object>(default, default, default!, default!, default!);
        
    }
}