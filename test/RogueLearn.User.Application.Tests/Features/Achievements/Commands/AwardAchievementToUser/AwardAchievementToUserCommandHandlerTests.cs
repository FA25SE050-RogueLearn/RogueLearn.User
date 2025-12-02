using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_UserNotFound_Throws(AwardAchievementToUserCommand command)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new AwardAchievementToUserCommandValidator();
        var logger = Substitute.For<ILogger<AwardAchievementToUserCommandHandler>>();

        userRepo.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        var sut = new AwardAchievementToUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_AchievementNotFound_Throws(AwardAchievementToUserCommand command)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new AwardAchievementToUserCommandValidator();
        var logger = Substitute.For<ILogger<AwardAchievementToUserCommandHandler>>();

        var user = new UserProfile { Id = command.UserId, AuthUserId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.GetByIdAsync(command.AchievementId, Arg.Any<CancellationToken>()).Returns((Achievement?)null);

        var sut = new AwardAchievementToUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_DuplicateAward_Throws(AwardAchievementToUserCommand command)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new AwardAchievementToUserCommandValidator();
        var logger = Substitute.For<ILogger<AwardAchievementToUserCommandHandler>>();

        var user = new UserProfile { Id = command.UserId, AuthUserId = Guid.NewGuid(), Username = "u" };
        var ach = new Achievement { Id = command.AchievementId, Name = "a" };
        userRepo.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.GetByIdAsync(command.AchievementId, Arg.Any<CancellationToken>()).Returns(ach);
        uaRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);

        var sut = new AwardAchievementToUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_AddsUserAchievement(AwardAchievementToUserCommand command)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new AwardAchievementToUserCommandValidator();
        var logger = Substitute.For<ILogger<AwardAchievementToUserCommandHandler>>();

        var user = new UserProfile { Id = command.UserId, AuthUserId = Guid.NewGuid(), Username = "u" };
        var ach = new Achievement { Id = command.AchievementId, Name = "a" };
        userRepo.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.GetByIdAsync(command.AchievementId, Arg.Any<CancellationToken>()).Returns(ach);
        uaRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = new AwardAchievementToUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await sut.Handle(command, CancellationToken.None);
        await uaRepo.Received(1).AddAsync(Arg.Any<UserAchievement>(), Arg.Any<CancellationToken>());
    }
}