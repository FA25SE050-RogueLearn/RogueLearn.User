using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.GrantGuildAchievement;
using RogueLearn.User.Application.Features.Guilds.Commands.UpdateGuildMeritPoints;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.GrantGuildAchievement;

public class GrantGuildAchievementCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_WithMeritPoints_SendsUpdate(GrantGuildAchievementCommand cmd)
    {
        var achRepo = Substitute.For<IAchievementRepository>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GrantGuildAchievementCommandHandler>>();
        var sut = new GrantGuildAchievementCommandHandler(achRepo, mediator, logger);

        var ach = new Achievement { Key = cmd.AchievementKey, MeritPointsReward = 10 };
        achRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
               .Returns(ach);

        await sut.Handle(cmd, CancellationToken.None);
    }

    [Theory]
    [AutoData]
    public async Task Handle_NoMeritPoints_DoesNotSend(GrantGuildAchievementCommand cmd)
    {
        var achRepo = Substitute.For<IAchievementRepository>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GrantGuildAchievementCommandHandler>>();
        var sut = new GrantGuildAchievementCommandHandler(achRepo, mediator, logger);

        var ach = new Achievement { Key = cmd.AchievementKey, MeritPointsReward = 0 };
        achRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
               .Returns(ach);

        await sut.Handle(cmd, CancellationToken.None);
    }
}