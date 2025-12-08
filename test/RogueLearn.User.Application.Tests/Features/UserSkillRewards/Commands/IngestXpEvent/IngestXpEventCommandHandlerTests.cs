using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_IdempotentEvent_ReturnsNotProcessed()
    {
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);

        var skillId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var existing = new UserSkillReward { Id = Guid.NewGuid(), SkillId = skillId, SkillName = "Skill" };
        rewardRepo.GetBySourceAndSkillAsync(Arg.Any<Guid>(), Arg.Any<string>(), sourceId, skillId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var cmd = new IngestXpEventCommand
        {
            AuthUserId = Guid.NewGuid(),
            SkillId = skillId,
            SourceService = "svc",
            SourceType = "QuestComplete",
            SourceId = sourceId,
            Points = 50,
            Reason = "r"
        };

        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Processed.Should().BeFalse();
        res.RewardId.Should().Be(existing.Id);
        await rewardRepo.Received(0).AddAsync(Arg.Any<UserSkillReward>(), Arg.Any<CancellationToken>());
        await userSkillRepo.Received(0).AddAsync(Arg.Any<UserSkill>(), Arg.Any<CancellationToken>());
        await userSkillRepo.Received(0).UpdateAsync(Arg.Any<UserSkill>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingSkillId_ThrowsBadRequest()
    {
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);

        var cmd = new IngestXpEventCommand
        {
            AuthUserId = Guid.NewGuid(),
            SkillId = null,
            SourceService = "svc",
            SourceType = "QuestComplete",
            SourceId = Guid.NewGuid(),
            Points = 10,
            Reason = "r"
        };

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }

    [Fact]
    public async Task Handle_InvalidSourceType_UsesDefault_AndCreatesNewUserSkill()
    {
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);

        var skillId = Guid.NewGuid();
        skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(new Skill { Id = skillId, Name = "Coding" });
        userSkillRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserSkill, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((UserSkill?)null);

        var cmd = new IngestXpEventCommand
        {
            AuthUserId = Guid.NewGuid(),
            SkillId = skillId,
            SourceService = "svc",
            SourceType = "invalid",
            SourceId = Guid.NewGuid(),
            Points = 500,
            Reason = "good"
        };

        await sut.Handle(cmd, CancellationToken.None);
        await rewardRepo.Received(1).AddAsync(Arg.Is<UserSkillReward>(r => r.SourceType == RogueLearn.User.Domain.Enums.SkillRewardSourceType.QuestComplete && r.SkillId == skillId && r.SkillName == "Coding"), Arg.Any<CancellationToken>());
        await userSkillRepo.Received(1).AddAsync(Arg.Is<UserSkill>(s => s.SkillId == skillId && s.ExperiencePoints == 500 && s.Level == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingUserSkill_AccumulatesPoints_AndRecalculatesLevel()
    {
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);

        var skillId = Guid.NewGuid();
        skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(new Skill { Id = skillId, Name = "Coding" });
        var existingSkill = new UserSkill { AuthUserId = Guid.NewGuid(), SkillId = skillId, SkillName = "Coding", ExperiencePoints = 900, Level = 1 };
        userSkillRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserSkill, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(existingSkill);

        var cmd = new IngestXpEventCommand
        {
            AuthUserId = existingSkill.AuthUserId,
            SkillId = skillId,
            SourceService = "svc",
            SourceType = "QuestComplete",
            SourceId = Guid.NewGuid(),
            Points = 200,
            Reason = "more"
        };

        await sut.Handle(cmd, CancellationToken.None);
        await userSkillRepo.Received(1).UpdateAsync(Arg.Is<UserSkill>(s => s.ExperiencePoints == 1100 && s.Level == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativePoints_NewUserSkill_SetsZeroXp_LevelOne()
    {
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);

        var skillId = Guid.NewGuid();
        skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(new Skill { Id = skillId, Name = "Coding" });
        userSkillRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserSkill, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((UserSkill?)null);

        var cmd = new IngestXpEventCommand
        {
            AuthUserId = Guid.NewGuid(),
            SkillId = skillId,
            SourceService = "svc",
            SourceType = "QuestComplete",
            SourceId = Guid.NewGuid(),
            Points = -25,
            Reason = "bad"
        };

        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Processed.Should().BeTrue();
        res.NewExperiencePoints.Should().Be(0);
        res.NewLevel.Should().Be(1);
        await userSkillRepo.Received(1).AddAsync(Arg.Is<UserSkill>(s => s.ExperiencePoints == 0 && s.Level == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullSourceType_UsesDefaultAndRespectsOccurredAt()
    {
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);

        var skillId = Guid.NewGuid();
        skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(new Skill { Id = skillId, Name = "Coding" });
        userSkillRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserSkill, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((UserSkill?)null);

        var occurred = DateTimeOffset.UtcNow.AddDays(-1);
        var cmd = new IngestXpEventCommand
        {
            AuthUserId = Guid.NewGuid(),
            SkillId = skillId,
            SourceService = "svc",
            SourceType = null!,
            SourceId = Guid.NewGuid(),
            Points = 100,
            Reason = "ok",
            OccurredAt = occurred
        };

        await sut.Handle(cmd, CancellationToken.None);
        await rewardRepo.Received(1).AddAsync(Arg.Is<UserSkillReward>(r => r.SourceType == RogueLearn.User.Domain.Enums.SkillRewardSourceType.QuestComplete && r.CreatedAt == occurred), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_UnknownSkillId_ThrowsBadRequest()
    {
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);

        var skillId = Guid.NewGuid();
        skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns((Skill?)null);

        var cmd = new IngestXpEventCommand
        {
            AuthUserId = Guid.NewGuid(),
            SkillId = skillId,
            SourceService = "svc",
            SourceType = "QuestComplete",
            SourceId = Guid.NewGuid(),
            Points = 10,
            Reason = "r"
        };

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }
}

