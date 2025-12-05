using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Achievements.Commands.AwardAchievementToUser;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.AwardAchievementToUser;

public class AwardAchievementToUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserNotFound_Throws()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new AwardAchievementToUserCommandValidator();
        var logger = Substitute.For<ILogger<AwardAchievementToUserCommandHandler>>();

        var cmd = new AwardAchievementToUserCommand { UserId = Guid.NewGuid(), AchievementId = Guid.NewGuid() };
        userRepo.GetByIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        var sut = new AwardAchievementToUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AchievementNotFound_Throws()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new AwardAchievementToUserCommandValidator();
        var logger = Substitute.For<ILogger<AwardAchievementToUserCommandHandler>>();

        var cmd = new AwardAchievementToUserCommand { UserId = Guid.NewGuid(), AchievementId = Guid.NewGuid() };
        var user = new UserProfile { Id = cmd.UserId, AuthUserId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.GetByIdAsync(cmd.AchievementId, Arg.Any<CancellationToken>()).Returns((Achievement?)null);

        var sut = new AwardAchievementToUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateAward_Throws()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new AwardAchievementToUserCommandValidator();
        var logger = Substitute.For<ILogger<AwardAchievementToUserCommandHandler>>();

        var cmd = new AwardAchievementToUserCommand { UserId = Guid.NewGuid(), AchievementId = Guid.NewGuid() };
        var user = new UserProfile { Id = cmd.UserId, AuthUserId = Guid.NewGuid(), Username = "u" };
        var ach = new Achievement { Id = cmd.AchievementId, Name = "a" };
        userRepo.GetByIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.GetByIdAsync(cmd.AchievementId, Arg.Any<CancellationToken>()).Returns(ach);
        uaRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);

        var sut = new AwardAchievementToUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_AddsUserAchievement()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new AwardAchievementToUserCommandValidator();
        var logger = Substitute.For<ILogger<AwardAchievementToUserCommandHandler>>();

        var cmd = new AwardAchievementToUserCommand { UserId = Guid.NewGuid(), AchievementId = Guid.NewGuid() };
        var user = new UserProfile { Id = cmd.UserId, AuthUserId = Guid.NewGuid(), Username = "u" };
        var ach = new Achievement { Id = cmd.AchievementId, Name = "a" };
        userRepo.GetByIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.GetByIdAsync(cmd.AchievementId, Arg.Any<CancellationToken>()).Returns(ach);
        uaRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = new AwardAchievementToUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await sut.Handle(cmd, CancellationToken.None);
        await uaRepo.Received(1).AddAsync(Arg.Any<UserAchievement>(), Arg.Any<CancellationToken>());
    }
}