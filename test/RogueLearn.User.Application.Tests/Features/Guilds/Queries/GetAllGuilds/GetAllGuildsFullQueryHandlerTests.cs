using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Queries.GetAllGuilds;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Queries.GetAllGuilds;

public class GetAllGuildsFullQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMapped()
    {
        var repo = Substitute.For<IGuildRepository>();
        var sut = new GetAllGuildsFullQueryHandler(repo);
        var list = new List<Guild> { new() { Id = System.Guid.NewGuid(), Name = "G", GuildType = RogueLearn.User.Domain.Enums.GuildType.Academic, MaxMembers = 10, CurrentMemberCount = 1, MeritPoints = 0, IsPublic = true } };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(list);
        var result = await sut.Handle(new GetAllGuildsFullQuery(), CancellationToken.None);
        result.Count.Should().Be(1);
        result.First().GuildType.Should().Be(RogueLearn.User.Domain.Enums.GuildType.Academic);
    }
}