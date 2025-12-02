using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;

public class GetSubjectSkillMappingsQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoMappings_ReturnsEmpty()
    {
        var repo = Substitute.For<ISubjectSkillMappingRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var sut = new GetSubjectSkillMappingsQueryHandler(repo, skillRepo, depRepo);

        var subjectId = Guid.NewGuid();
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<SubjectSkillMapping>());
        var res = await sut.Handle(new GetSubjectSkillMappingsQuery { SubjectId = subjectId }, CancellationToken.None);
        res.Should().NotBeNull();
        res.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsMappings_WithPrerequisites()
    {
        var repo = Substitute.For<ISubjectSkillMappingRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var sut = new GetSubjectSkillMappingsQueryHandler(repo, skillRepo, depRepo);

        var subjectId = Guid.NewGuid();
        var skillA = Guid.NewGuid();
        var skillB = Guid.NewGuid();
        var preX = Guid.NewGuid();

        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<SubjectSkillMapping>
        {
            new() { Id = Guid.NewGuid(), SubjectId = subjectId, SkillId = skillA, RelevanceWeight = 1.0m, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), SubjectId = subjectId, SkillId = skillB, RelevanceWeight = 0.8m, CreatedAt = DateTimeOffset.UtcNow }
        });

        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Skill>
        {
            new() { Id = skillA, Name = "A" },
            new() { Id = skillB, Name = "B" },
            new() { Id = preX, Name = "X" }
        });

        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SkillDependency>
        {
            new() { Id = Guid.NewGuid(), SkillId = skillA, PrerequisiteSkillId = preX },
            new() { Id = Guid.NewGuid(), SkillId = skillB, PrerequisiteSkillId = skillA },
        });

        var res = await sut.Handle(new GetSubjectSkillMappingsQuery { SubjectId = subjectId }, CancellationToken.None);

        res.Should().HaveCount(2);
        var a = res.First(r => r.SkillId == skillA);
        a.SkillName.Should().Be("A");
        a.Prerequisites.Should().ContainSingle(p => p.PrerequisiteSkillId == preX && p.PrerequisiteSkillName == "X");

        var b = res.First(r => r.SkillId == skillB);
        b.Prerequisites.Should().ContainSingle(p => p.PrerequisiteSkillId == skillA && p.PrerequisiteSkillName == "A");
    }
}