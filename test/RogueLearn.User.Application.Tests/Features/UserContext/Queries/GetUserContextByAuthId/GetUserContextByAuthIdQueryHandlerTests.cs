using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.UserContext.Queries.GetUserContextByAuthId;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.UserContext.Queries.GetUserContextByAuthId;

public class GetUserContextByAuthIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsContext()
    {
        var service = Substitute.For<IUserContextService>();
        var logger = Substitute.For<ILogger<GetUserContextByAuthIdQueryHandler>>();
        var authId = Guid.NewGuid();
        var expected = new UserContextDto { AuthUserId = authId, Username = "u" };
        service.BuildForAuthUserAsync(authId, Arg.Any<CancellationToken>()).Returns(expected);

        var sut = new GetUserContextByAuthIdQueryHandler(service, logger);
        var res = await sut.Handle(new GetUserContextByAuthIdQuery(authId), CancellationToken.None);

        res.Should().NotBeNull();
        res!.AuthUserId.Should().Be(authId);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var service = Substitute.For<IUserContextService>();
        var logger = Substitute.For<ILogger<GetUserContextByAuthIdQueryHandler>>();
        var authId = Guid.NewGuid();
        service.BuildForAuthUserAsync(authId, Arg.Any<CancellationToken>()).Returns((UserContextDto?)null);

        var sut = new GetUserContextByAuthIdQueryHandler(service, logger);
        var res = await sut.Handle(new GetUserContextByAuthIdQuery(authId), CancellationToken.None);
        res.Should().BeNull();
    }
}