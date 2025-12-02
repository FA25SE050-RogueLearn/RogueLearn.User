using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Behaviours;

namespace RogueLearn.User.Application.Tests.Behaviours;

public class UnhandledExceptionBehaviourTests
{
    public record DummyRequest(string Payload) : IRequest<string>;
    private static Task<string> Throwing(CancellationToken ct) => Task.FromException<string>(new InvalidOperationException("boom"));

    [Fact]
    public async Task Handle_When_Next_Throws_Logs_And_Rethrows()
    {
        var logger = Substitute.For<ILogger<UnhandledExceptionBehaviour<DummyRequest, string>>>();
        var sut = new UnhandledExceptionBehaviour<DummyRequest, string>(logger);
        var request = new DummyRequest("boom");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(request, new RequestHandlerDelegate<string>(Throwing), CancellationToken.None));

        logger.ReceivedWithAnyArgs(1).Log<object>(default, default, default!, default!, default!);
    }
}