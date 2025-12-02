using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetGuildById;

public class GetGuildByIdQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(GetGuildByIdQuery query)
    {
        var repo = Substitute.For<IGuildRepository>();
        var sut = new GetGuildByIdQueryHandler(repo);
        repo.GetByIdAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(query, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_ReturnsDto(GetGuildByIdQuery query)
    {
        var repo = Substitute.For<IGuildRepository>();
        var sut = new GetGuildByIdQueryHandler(repo);
        var g = new Guild { Id = query.GuildId, Name = "G", Description = "D", IsPublic = true, IsLecturerGuild = false, MaxMembers = 50, CreatedBy = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        repo.GetByIdAsync(query.GuildId, Arg.Any<CancellationToken>()).Returns(g);
        var result = await sut.Handle(query, CancellationToken.None);
        result.Id.Should().Be(g.Id);
        result.Name.Should().Be("G");
    }
}