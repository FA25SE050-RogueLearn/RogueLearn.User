using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Skills.Queries.GetSkillDetail;

public class GetSkillDetailQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_SkillMissing_ReturnsNull(GetSkillDetailQuery query)
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSkillDetailQueryHandler>>();
        var sut = new GetSkillDetailQueryHandler(skillRepo, userSkillRepo, depRepo, mappingRepo, questRepo, subjectRepo, logger);

        skillRepo.GetByIdAsync(query.SkillId, Arg.Any<CancellationToken>()).Returns((Skill?)null);
        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public async Task Handle_MapsLearningPath(GetSkillDetailQuery query)
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<GetSkillDetailQueryHandler>>();
        var sut = new GetSkillDetailQueryHandler(skillRepo, userSkillRepo, depRepo, mappingRepo, questRepo, subjectRepo, logger);

        var skill = new Skill { Id = query.SkillId, Name = "Skill", Tier = RogueLearn.User.Domain.Enums.SkillTierLevel.Foundation };
        skillRepo.GetByIdAsync(query.SkillId, Arg.Any<CancellationToken>()).Returns(skill);
        userSkillRepo.GetSkillsByAuthIdAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserSkill>());
        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SkillDependency>());

        var subjectId = System.Guid.NewGuid();
        mappingRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SubjectSkillMapping> { new() { SubjectId = subjectId, SkillId = query.SkillId } });
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Quest> { new() { Id = System.Guid.NewGuid(), Title = "Q1", SubjectId = subjectId, IsActive = true, ExperiencePointsReward = 50 } });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().NotBeNull();
        result!.LearningPath.Should().HaveCount(1);
        result.LearningPath[0].Title.Should().Be("Q1");
    }
}