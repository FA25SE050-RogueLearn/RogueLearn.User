using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsContext(GetUserContextByAuthIdQuery query)
    {
        var service = Substitute.For<IUserContextService>();
        var logger = Substitute.For<ILogger<GetUserContextByAuthIdQueryHandler>>();
        var expected = new UserContextDto { AuthUserId = query.AuthId, Username = "u" };
        service.BuildForAuthUserAsync(query.AuthId, Arg.Any<CancellationToken>()).Returns(expected);

        var sut = new GetUserContextByAuthIdQueryHandler(service, logger);
        var res = await sut.Handle(query, CancellationToken.None);

        res.Should().NotBeNull();
        res!.AuthUserId.Should().Be(query.AuthId);
    }

    [Theory]
    [AutoData]
    public async Task Handle_NotFound_ReturnsNull(GetUserContextByAuthIdQuery query)
    {
        var service = Substitute.For<IUserContextService>();
        var logger = Substitute.For<ILogger<GetUserContextByAuthIdQueryHandler>>();
        service.BuildForAuthUserAsync(query.AuthId, Arg.Any<CancellationToken>()).Returns((UserContextDto?)null);

        var sut = new GetUserContextByAuthIdQueryHandler(service, logger);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Should().BeNull();
    }
}