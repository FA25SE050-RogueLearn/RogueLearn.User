using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    [Fact]
    public async Task Handle_ReturnsNodesAndDependencies()
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var sut = new GetSkillTreeQueryHandler(skillRepo, userSkillRepo, depRepo);

        var skill = new Skill { Id = System.Guid.NewGuid(), Name = "C#", Domain = "Programming", Tier = SkillTierLevel.Foundation, Description = "Basics" };
        var userSkill = new UserSkill { SkillId = skill.Id, Level = 3, ExperiencePoints = 2500 };
        var dep = new SkillDependency { SkillId = skill.Id, PrerequisiteSkillId = System.Guid.NewGuid(), RelationshipType = SkillRelationshipType.Prerequisite };

        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Skill> { skill });
        var query = new GetSkillTreeQuery { AuthUserId = System.Guid.NewGuid() };
        userSkillRepo.GetSkillsByAuthIdAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserSkill> { userSkill });
        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SkillDependency> { dep });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Nodes.Should().HaveCount(1);
        result.Nodes[0].UserLevel.Should().Be(3);
        result.Dependencies.Should().HaveCount(1);
        result.Dependencies[0].SkillId.Should().Be(skill.Id);
    }

    [Fact]
    public async Task Handle_EmptyRepositories_ReturnsEmptyTree()
    {
        var skillRepo = Substitute.For<ISkillRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var sut = new GetSkillTreeQueryHandler(skillRepo, userSkillRepo, depRepo);

        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Skill>());
        userSkillRepo.GetSkillsByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserSkill>());
        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SkillDependency>());

        var result = await sut.Handle(new GetSkillTreeQuery { AuthUserId = System.Guid.NewGuid() }, CancellationToken.None);
        result.Nodes.Should().BeEmpty();
        result.Dependencies.Should().BeEmpty();
    }
}
