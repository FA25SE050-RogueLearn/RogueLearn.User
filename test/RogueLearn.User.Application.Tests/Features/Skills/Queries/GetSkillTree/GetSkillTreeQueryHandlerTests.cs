using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillTree;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Skills.Queries.GetSkillTree;

public class GetSkillTreeQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsNodesAndDependencies(GetSkillTreeQuery query)
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var sut = new GetSkillTreeQueryHandler(skillRepo, userSkillRepo, depRepo);

        var skill = new Skill { Id = System.Guid.NewGuid(), Name = "C#", Domain = "Programming", Tier = SkillTierLevel.Foundation, Description = "Basics" };
        var userSkill = new UserSkill { SkillId = skill.Id, Level = 3, ExperiencePoints = 2500 };
        var dep = new SkillDependency { SkillId = skill.Id, PrerequisiteSkillId = System.Guid.NewGuid(), RelationshipType = SkillRelationshipType.Prerequisite };

        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Skill> { skill });
        userSkillRepo.GetSkillsByAuthIdAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserSkill> { userSkill });
        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SkillDependency> { dep });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Nodes.Should().HaveCount(1);
        result.Nodes[0].UserLevel.Should().Be(3);
        result.Dependencies.Should().HaveCount(1);
        result.Dependencies[0].SkillId.Should().Be(skill.Id);
    }
}