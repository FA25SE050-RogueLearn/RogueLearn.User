using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Achievements.Commands.RevokeAchievementFromUser;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.RevokeAchievementFromUser;

public class RevokeAchievementFromUserCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_UserNotFound_Throws(RevokeAchievementFromUserCommand command)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new RevokeAchievementFromUserCommandValidator();
        var logger = Substitute.For<ILogger<RevokeAchievementFromUserCommandHandler>>();

        userRepo.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        var sut = new RevokeAchievementFromUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_AchievementNotFound_Throws(RevokeAchievementFromUserCommand command)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new RevokeAchievementFromUserCommandValidator();
        var logger = Substitute.For<ILogger<RevokeAchievementFromUserCommandHandler>>();

        var user = new UserProfile { Id = command.UserId, AuthUserId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.GetByIdAsync(command.AchievementId, Arg.Any<CancellationToken>()).Returns((Achievement?)null);

        var sut = new RevokeAchievementFromUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_NoExistingUserAchievement_ThrowsBadRequest(RevokeAchievementFromUserCommand command)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new RevokeAchievementFromUserCommandValidator();
        var logger = Substitute.For<ILogger<RevokeAchievementFromUserCommandHandler>>();

        var user = new UserProfile { Id = command.UserId, AuthUserId = Guid.NewGuid(), Username = "u" };
        var ach = new Achievement { Id = command.AchievementId, Name = "a" };
        userRepo.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.GetByIdAsync(command.AchievementId, Arg.Any<CancellationToken>()).Returns(ach);
        uaRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<UserAchievement>());

        var sut = new RevokeAchievementFromUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_DeletesUserAchievement(RevokeAchievementFromUserCommand command)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var validator = new RevokeAchievementFromUserCommandValidator();
        var logger = Substitute.For<ILogger<RevokeAchievementFromUserCommandHandler>>();

        var user = new UserProfile { Id = command.UserId, AuthUserId = Guid.NewGuid(), Username = "u" };
        var ach = new Achievement { Id = command.AchievementId, Name = "a" };
        var ua = new UserAchievement { Id = Guid.NewGuid(), AuthUserId = user.AuthUserId, AchievementId = ach.Id };
        userRepo.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.GetByIdAsync(command.AchievementId, Arg.Any<CancellationToken>()).Returns(ach);
        uaRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { ua });

        var sut = new RevokeAchievementFromUserCommandHandler(userRepo, achRepo, uaRepo, validator, logger);
        await sut.Handle(command, CancellationToken.None);
        await uaRepo.Received(1).DeleteAsync(ua.Id, Arg.Any<CancellationToken>());
    }
}