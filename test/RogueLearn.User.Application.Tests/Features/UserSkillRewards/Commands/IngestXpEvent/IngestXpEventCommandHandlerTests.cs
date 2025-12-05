using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_Idempotent_ReturnsProcessedFalse()
    {
        var cmd = new IngestXpEventCommand { AuthUserId = Guid.NewGuid(), SourceService = "svc", SourceId = Guid.NewGuid(), SkillId = Guid.NewGuid(), Points = 100 };

        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();

        rewardRepo.GetBySourceAndSkillAsync(cmd.AuthUserId, cmd.SourceService!, cmd.SourceId!.Value, cmd.SkillId!.Value, Arg.Any<CancellationToken>())
            .Returns(new UserSkillReward { Id = Guid.NewGuid(), SkillName = "Skill" });

        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Processed.Should().BeFalse();
        res.Message.Should().Contain("already processed");
    }

    [Fact]
    public async Task Handle_SkillMissing_Throws()
    {
        var cmd = new IngestXpEventCommand { AuthUserId = Guid.NewGuid(), Points = 100, SkillId = null };
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_NewUserSkill()
    {
        var cmd = new IngestXpEventCommand { AuthUserId = Guid.NewGuid(), SkillId = Guid.NewGuid(), Points = 250, SourceService = "svc", SourceId = Guid.NewGuid() };
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();

        var skill = new Skill { Id = cmd.SkillId!.Value, Name = "SkillA" };
        skillRepo.GetByIdAsync(cmd.SkillId!.Value, Arg.Any<CancellationToken>()).Returns(skill);
        userSkillRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserSkill, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserSkill?)null);
        rewardRepo.AddAsync(Arg.Any<UserSkillReward>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserSkillReward>());
        userSkillRepo.AddAsync(Arg.Any<UserSkill>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserSkill>());

        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Processed.Should().BeTrue();
        res.SkillName.Should().Be("SkillA");
        res.NewLevel.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Handle_Success_UpdateExisting()
    {
        var cmd = new IngestXpEventCommand { AuthUserId = Guid.NewGuid(), SkillId = Guid.NewGuid(), Points = 500, SourceService = "svc", SourceId = Guid.NewGuid() };
        var rewardRepo = Substitute.For<IUserSkillRewardRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();

        var skill = new Skill { Id = cmd.SkillId!.Value, Name = "SkillA" };
        var current = new UserSkill { AuthUserId = cmd.AuthUserId, SkillId = skill.Id, SkillName = skill.Name, ExperiencePoints = 800, Level = 1 };
        skillRepo.GetByIdAsync(cmd.SkillId!.Value, Arg.Any<CancellationToken>()).Returns(skill);
        userSkillRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserSkill, bool>>>(), Arg.Any<CancellationToken>()).Returns(current);
        rewardRepo.AddAsync(Arg.Any<UserSkillReward>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserSkillReward>());
        userSkillRepo.UpdateAsync(Arg.Any<UserSkill>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserSkill>());

        var sut = new IngestXpEventCommandHandler(rewardRepo, userSkillRepo, skillRepo);
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.NewExperiencePoints.Should().Be(1300);
        res.NewLevel.Should().BeGreaterThanOrEqualTo(2);
    }
}