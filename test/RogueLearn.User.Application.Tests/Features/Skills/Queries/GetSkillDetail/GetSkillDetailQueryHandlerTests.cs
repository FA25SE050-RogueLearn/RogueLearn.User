using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Tests.Features.Skills.Queries.GetSkillDetail;

public class GetSkillDetailQueryHandlerTests
{
    [Fact]
    public async Task Handle_SkillNotFound_ReturnsNull()
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var mapRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSkillDetailQueryHandler>>();

        var sut = new GetSkillDetailQueryHandler(skillRepo, userSkillRepo, depRepo, mapRepo, questRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetSkillDetailQuery { AuthUserId = Guid.NewGuid(), SkillId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DependencyAndUnlocks_MapStatuses()
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var mapRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSkillDetailQueryHandler>>();

        var authId = Guid.NewGuid();
        var mainSkillId = Guid.NewGuid();
        var prereqId = Guid.NewGuid();
        var unlockId = Guid.NewGuid();

        skillRepo.GetByIdAsync(mainSkillId, Arg.Any<CancellationToken>()).Returns(new Skill { Id = mainSkillId, Name = "Main", Tier = SkillTierLevel.Intermediate });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Skill { Id = prereqId, Name = "Pre" },
            new Skill { Id = unlockId, Name = "Unl" }
        });

        // User has prereq at level 3 (<5), and main skill at level 5 (>=5)
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new UserSkill { AuthUserId = authId, SkillId = prereqId, Level = 3, ExperiencePoints = 3000 },
            new UserSkill { AuthUserId = authId, SkillId = mainSkillId, Level = 5, ExperiencePoints = 5000 }
        });

        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            // prereq: points TO main skill
            new SkillDependency { SkillId = mainSkillId, PrerequisiteSkillId = prereqId, RelationshipType = SkillRelationshipType.Prerequisite },
            // unlock: main skill points TO unlock skill
            new SkillDependency { SkillId = unlockId, PrerequisiteSkillId = mainSkillId, RelationshipType = SkillRelationshipType.Prerequisite }
        });

        mapRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());

        var sut = new GetSkillDetailQueryHandler(skillRepo, userSkillRepo, depRepo, mapRepo, questRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetSkillDetailQuery { AuthUserId = authId, SkillId = mainSkillId }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.Prerequisites.Should().HaveCount(1);
        res.Prerequisites[0].IsMet.Should().BeFalse();
        res.Prerequisites[0].StatusLabel.Should().Contain("Levels");
        res.Unlocks.Should().HaveCount(1);
        res.Unlocks[0].IsMet.Should().BeTrue();
        res.Unlocks[0].StatusLabel.Should().Be("Available");
    }
}
