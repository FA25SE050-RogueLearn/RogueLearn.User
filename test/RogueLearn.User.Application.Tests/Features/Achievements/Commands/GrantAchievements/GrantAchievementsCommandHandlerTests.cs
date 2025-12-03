using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Achievements.Commands.GrantAchievements;
using RogueLearn.User.Application.Features.Guilds.Commands.UpdateMemberContributionPoints;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.GrantAchievements;

public class GrantAchievementsCommandHandlerTests
{
    [Fact]
    public async Task Handle_InvalidAuthUserId_AddsError()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var gmRepo = Substitute.For<IGuildMemberRepository>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<GrantAchievementsCommandHandler>>();

        var cmd = new GrantAchievementsCommand
        {
            Entries = new List<GrantAchievementEntry>
            {
                new GrantAchievementEntry { AuthUserId = Guid.Empty, AchievementKey = "k1" }
            }
        };

        var sut = new GrantAchievementsCommandHandler(userRepo, achRepo, uaRepo, gmRepo, mediator, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.GrantedCount.Should().Be(0);
        res.Errors.Should().ContainSingle().Which.Should().Contain("invalid auth_user_id");
    }

    [Fact]
    public async Task Handle_UserNotFound_AddsError()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var gmRepo = Substitute.For<IGuildMemberRepository>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<GrantAchievementsCommandHandler>>();

        var entry = new GrantAchievementEntry { AuthUserId = Guid.NewGuid(), AchievementKey = "key" };
        userRepo.GetByAuthIdAsync(entry.AuthUserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        var cmd = new GrantAchievementsCommand { Entries = new List<GrantAchievementEntry> { entry } };
        var sut = new GrantAchievementsCommandHandler(userRepo, achRepo, uaRepo, gmRepo, mediator, logger);

        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Errors.Should().Contain(x => x.Contains("user not found"));
        res.GrantedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_AchievementNotFound_AddsError()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var gmRepo = Substitute.For<IGuildMemberRepository>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<GrantAchievementsCommandHandler>>();

        var entry = new GrantAchievementEntry { AuthUserId = Guid.NewGuid(), AchievementKey = "key" };
        var user = new UserProfile { AuthUserId = entry.AuthUserId };
        userRepo.GetByAuthIdAsync(entry.AuthUserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>()).Returns((Achievement?)null);

        var cmd = new GrantAchievementsCommand { Entries = new List<GrantAchievementEntry> { entry } };
        var sut = new GrantAchievementsCommandHandler(userRepo, achRepo, uaRepo, gmRepo, mediator, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Errors.Should().Contain(x => x.Contains("achievement not found"));
        res.GrantedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DuplicateNotAllowed_AddsError()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var gmRepo = Substitute.For<IGuildMemberRepository>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<GrantAchievementsCommandHandler>>();

        var entry = new GrantAchievementEntry { AuthUserId = Guid.NewGuid(), AchievementKey = "key" };
        var user = new UserProfile { AuthUserId = entry.AuthUserId };
        var achievement = new Achievement { Id = Guid.NewGuid(), Key = entry.AchievementKey, Name = "n", IsMedal = false };
        userRepo.GetByAuthIdAsync(entry.AuthUserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(achievement);
        uaRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new GrantAchievementsCommand { Entries = new List<GrantAchievementEntry> { entry } };
        var sut = new GrantAchievementsCommandHandler(userRepo, achRepo, uaRepo, gmRepo, mediator, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.Errors.Should().Contain(x => x.Contains("already earned"));
        res.GrantedCount.Should().Be(0);
        await uaRepo.DidNotReceive().AddAsync(Arg.Any<UserAchievement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_GrantsAndUpdatesContribution()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var achRepo = Substitute.For<IAchievementRepository>();
        var uaRepo = Substitute.For<IUserAchievementRepository>();
        var gmRepo = Substitute.For<IGuildMemberRepository>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<ILogger<GrantAchievementsCommandHandler>>();

        var entry = new GrantAchievementEntry { AuthUserId = Guid.NewGuid(), AchievementKey = "key" };
        var user = new UserProfile { AuthUserId = entry.AuthUserId };
        var achievement = new Achievement { Id = Guid.NewGuid(), Key = entry.AchievementKey, Name = "n", IsMedal = false, ContributionPointsReward = 5 };
        var memberships = new List<GuildMember>
        {
            new GuildMember { GuildId = Guid.NewGuid(), AuthUserId = user.AuthUserId, Status = MemberStatus.Active },
            new GuildMember { GuildId = Guid.NewGuid(), AuthUserId = user.AuthUserId, Status = MemberStatus.Left }
        };

        userRepo.GetByAuthIdAsync(entry.AuthUserId, Arg.Any<CancellationToken>()).Returns(user);
        achRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(achievement);
        uaRepo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);
        gmRepo.GetMembershipsByUserAsync(user.AuthUserId, Arg.Any<CancellationToken>()).Returns(memberships);

        var cmd = new GrantAchievementsCommand { Entries = new List<GrantAchievementEntry> { entry } };
        var sut = new GrantAchievementsCommandHandler(userRepo, achRepo, uaRepo, gmRepo, mediator, logger);

        var res = await sut.Handle(cmd, CancellationToken.None);
        res.GrantedCount.Should().Be(1);
        await uaRepo.Received(1).AddAsync(Arg.Any<UserAchievement>(), Arg.Any<CancellationToken>());
        await mediator.Received(1).Send(Arg.Any<UpdateMemberContributionPointsCommand>(), Arg.Any<CancellationToken>());
    }
}