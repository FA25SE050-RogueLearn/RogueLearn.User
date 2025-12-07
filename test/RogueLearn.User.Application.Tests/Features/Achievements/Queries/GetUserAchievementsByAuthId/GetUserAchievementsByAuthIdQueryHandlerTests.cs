using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Queries.GetUserAchievementsByAuthId;

public class GetUserAchievementsByAuthIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_SkipsMissingAchievements()
    {
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var logger = Substitute.For<ILogger<GetUserAchievementsByAuthIdQueryHandler>>();

        var query = new GetUserAchievementsByAuthIdQuery { AuthUserId = Guid.NewGuid() };
        var ua1 = new UserAchievement { AchievementId = Guid.NewGuid(), AuthUserId = query.AuthUserId, EarnedAt = DateTimeOffset.UtcNow };
        var ua2 = new UserAchievement { AchievementId = Guid.NewGuid(), AuthUserId = query.AuthUserId, EarnedAt = DateTimeOffset.UtcNow };
        uaRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserAchievement> { ua1, ua2 });

        achRepo.GetByIdAsync(ua1.AchievementId, Arg.Any<CancellationToken>()).Returns(new Achievement { Id = ua1.AchievementId, Key = "k1", Name = "n1" });
        achRepo.GetByIdAsync(ua2.AchievementId, Arg.Any<CancellationToken>()).Returns((Achievement?)null);

        var sut = new GetUserAchievementsByAuthIdQueryHandler(uaRepo, achRepo, logger);
        var res = await sut.Handle(query, CancellationToken.None);

        res.Achievements.Should().HaveCount(1);
        res.Achievements[0].Key.Should().Be("k1");
    }
}