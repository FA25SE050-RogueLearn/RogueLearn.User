using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Achievements.Queries.GetAllAchievements;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Queries.GetAllAchievements;

public class GetAllAchievementsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedList()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllAchievementsQueryHandler>>();

        var achievements = new List<Achievement>
        {
            new Achievement { Id = System.Guid.NewGuid(), Key = "k1", Name = "n1" },
            new Achievement { Id = System.Guid.NewGuid(), Key = "k2", Name = "n2" }
        };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(achievements);
        mapper.Map<List<AchievementDto>>(achievements).Returns(new List<AchievementDto>
        {
            new AchievementDto { Id = achievements[0].Id, Key = achievements[0].Key, Name = achievements[0].Name },
            new AchievementDto { Id = achievements[1].Id, Key = achievements[1].Key, Name = achievements[1].Name }
        });

        var sut = new GetAllAchievementsQueryHandler(repo, mapper, logger);
        var res = await sut.Handle(new GetAllAchievementsQuery(), CancellationToken.None);

        res.Achievements.Should().HaveCount(2);
        res.Achievements[0].Key.Should().Be("k1");
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmptyList()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllAchievementsQueryHandler>>();

        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Achievement>());
        mapper.Map<List<AchievementDto>>(Arg.Any<List<Achievement>>()).Returns((List<AchievementDto>?)null);

        var sut = new GetAllAchievementsQueryHandler(repo, mapper, logger);
        var res = await sut.Handle(new GetAllAchievementsQuery(), CancellationToken.None);

        res.Achievements.Should().NotBeNull();
        res.Achievements.Should().BeEmpty();
    }
}