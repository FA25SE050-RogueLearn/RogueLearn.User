using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Skills.Queries.GetSkillDetail;

public class GetSkillDetailQueryHandlerTests
{
    [Fact]
    public async Task Handle_FiltersBySkillIdAndSubjectHasValue()
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetSkillDetailQueryHandler>>();

        var skillId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var skill = new Skill { Id = skillId, Name = "S", Tier = RogueLearn.User.Domain.Enums.SkillTierLevel.Foundation };
        skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(skill);
        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserSkill>());
        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<SkillDependency>());

        var subjectId = Guid.NewGuid();
        mappingRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SkillId = skillId, SubjectId = subjectId }, new SubjectSkillMapping { SkillId = Guid.NewGuid(), SubjectId = Guid.NewGuid() } });
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Quest { Id = Guid.NewGuid(), SubjectId = subjectId, IsActive = true, Title = "Q1" },
            new Quest { Id = Guid.NewGuid(), SubjectId = subjectId, IsActive = false, Title = "Q2" },
            new Quest { Id = Guid.NewGuid(), SubjectId = null, IsActive = true, Title = "Q3" }
        });

        var sut = new GetSkillDetailQueryHandler(skillRepo, userSkillRepo, depRepo, mappingRepo, questRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetSkillDetailQuery { SkillId = skillId, AuthUserId = authId }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.LearningPath.Should().HaveCount(1);
        res.LearningPath[0].Title.Should().Be("Q1");
    }

    [Fact]
    public async Task Handle_SkillNotFound_ReturnsNull()
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetSkillDetailQueryHandler>>();

        var sut = new GetSkillDetailQueryHandler(skillRepo, userSkillRepo, depRepo, mappingRepo, questRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetSkillDetailQuery { SkillId = Guid.NewGuid(), AuthUserId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PrerequisitesAndUnlocks_StatusesReflectMasteryThreshold()
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetSkillDetailQueryHandler>>();

        var skillId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var skill = new Skill { Id = skillId, Name = "S", Tier = RogueLearn.User.Domain.Enums.SkillTierLevel.Foundation };
        skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(skill);

        var prereqId = Guid.NewGuid();
        var unlockId = Guid.NewGuid();
        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new SkillDependency { SkillId = skillId, PrerequisiteSkillId = prereqId }, new SkillDependency { SkillId = unlockId, PrerequisiteSkillId = skillId } });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Skill { Id = prereqId, Name = "P" }, new Skill { Id = unlockId, Name = "U" }, skill });

        userSkillRepo.GetSkillsByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new[] { new UserSkill { SkillId = skillId, Level = 4, ExperiencePoints = 3500 }, new UserSkill { SkillId = prereqId, Level = 5, ExperiencePoints = 5000 } });

        mappingRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());

        var sut = new GetSkillDetailQueryHandler(skillRepo, userSkillRepo, depRepo, mappingRepo, questRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetSkillDetailQuery { SkillId = skillId, AuthUserId = authId }, CancellationToken.None);

        res!.Prerequisites.Should().Contain(p => p.SkillId == prereqId && p.IsMet);
        res.Unlocks.Should().Contain(u => u.SkillId == unlockId && !u.IsMet);
        res.ProgressPercentage.Should().Be( res.XpProgressInLevel * 100.0 / res.XpForNextLevel );
    }
}
