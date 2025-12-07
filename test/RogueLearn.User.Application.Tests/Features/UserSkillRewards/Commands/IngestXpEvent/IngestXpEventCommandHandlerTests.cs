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

