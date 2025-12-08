using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Queries.GetAllGuilds;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetAllGuilds;

public class GetAllGuildsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMapped()
    {
        var repo = Substitute.For<IGuildRepository>();
        var sut = new GetAllGuildsQueryHandler(repo);
        var list = new List<Guild> { new() { Id = System.Guid.NewGuid(), Name = "G", IsPublic = true, IsLecturerGuild = false, MaxMembers = 10, CreatedAt = System.DateTimeOffset.UtcNow, CreatedBy = System.Guid.NewGuid(), CurrentMemberCount = 1 } };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(list);
        var result = await sut.Handle(new GetAllGuildsQuery(), CancellationToken.None);
        result.Count().Should().Be(1);
        result.First().Name.Should().Be("G");
    }
}