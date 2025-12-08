using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestSkills;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetQuestSkills;

public class GetQuestSkillsQueryHandlerTests
{
    [Fact]
    public async Task QuestNotFound_ReturnsNull()
    {
        var sut = new GetQuestSkillsQueryHandler(Substitute.For<IQuestRepository>(), Substitute.For<ISubjectRepository>(), Substitute.For<ISubjectSkillMappingRepository>(), Substitute.For<ISkillRepository>(), Substitute.For<ISkillDependencyRepository>());
        var res = await sut.Handle(new GetQuestSkillsQuery { QuestId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task NoSubject_ReturnsBasicResponse()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var q = new Quest { Id = Guid.NewGuid(), SubjectId = null };
        questRepo.GetByIdAsync(q.Id, Arg.Any<CancellationToken>()).Returns(q);
        var sut = new GetQuestSkillsQueryHandler(questRepo, Substitute.For<ISubjectRepository>(), Substitute.For<ISubjectSkillMappingRepository>(), Substitute.For<ISkillRepository>(), Substitute.For<ISkillDependencyRepository>());
        var res = await sut.Handle(new GetQuestSkillsQuery { QuestId = q.Id }, CancellationToken.None);
        res!.SubjectId.Should().BeNull();
        res.Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task SkillsAndPrereqs_Populated_WithUnknownFallbacks()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();
        var subjId = Guid.NewGuid();
        var q = new Quest { Id = Guid.NewGuid(), SubjectId = subjId };
        questRepo.GetByIdAsync(q.Id, Arg.Any<CancellationToken>()).Returns(q);
        subjectRepo.GetByIdAsync(subjId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjId, SubjectName = "S" });
        var sk1 = Guid.NewGuid();
        var sk2 = Guid.NewGuid();
        mappingRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new SubjectSkillMapping { SubjectId = subjId, SkillId = sk1, RelevanceWeight = 0.8m },
            new SubjectSkillMapping { SubjectId = subjId, SkillId = sk2, RelevanceWeight = 0.2m }
        });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Skill { Id = sk1, Name = "Skill1", Domain = "D" }
        });
        var prereq = Guid.NewGuid();
        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new SkillDependency { SkillId = sk1, PrerequisiteSkillId = prereq },
            new SkillDependency { SkillId = sk2, PrerequisiteSkillId = prereq }
        });
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Skill { Id = sk1, Name = "Skill1", Domain = "D" }
        });
        var sut = new GetQuestSkillsQueryHandler(questRepo, subjectRepo, mappingRepo, skillRepo, depRepo);
        var res = await sut.Handle(new GetQuestSkillsQuery { QuestId = q.Id }, CancellationToken.None);
        res!.Skills.Should().HaveCount(2);
        res.Skills![0].Prerequisites![0].SkillName.Should().Be("Unknown");
    }

    [Fact]
    public async Task NoMappings_ReturnsResponseWithoutSkills()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();

        var subjId = Guid.NewGuid();
        var q = new Quest { Id = Guid.NewGuid(), SubjectId = subjId };
        questRepo.GetByIdAsync(q.Id, Arg.Any<CancellationToken>()).Returns(q);
        subjectRepo.GetByIdAsync(subjId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjId, SubjectName = "Subject" });
        mappingRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<SubjectSkillMapping>());

        var sut = new GetQuestSkillsQueryHandler(questRepo, subjectRepo, mappingRepo, skillRepo, depRepo);
        var res = await sut.Handle(new GetQuestSkillsQuery { QuestId = q.Id }, CancellationToken.None);
        res!.SubjectId.Should().Be(subjId);
        res.SubjectName.Should().Be("Subject");
        res.Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task AdditionalPrereqSkills_AreResolvedFromExtraLookup()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();

        var subjId = Guid.NewGuid();
        var q = new Quest { Id = Guid.NewGuid(), SubjectId = subjId };
        questRepo.GetByIdAsync(q.Id, Arg.Any<CancellationToken>()).Returns(q);
        subjectRepo.GetByIdAsync(subjId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjId, SubjectName = "S" });

        var sk1 = Guid.NewGuid();
        var prereq = Guid.NewGuid();
        mappingRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new SubjectSkillMapping { SubjectId = subjId, SkillId = sk1, RelevanceWeight = 1.0m }
        });

        depRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new SkillDependency { SkillId = sk1, PrerequisiteSkillId = prereq }
        });

        // First GetAllAsync() returns only the mapped skill; second call (inside extra lookup) returns the prerequisite
        skillRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<Skill>>(new[] { new Skill { Id = sk1, Name = "Mapped", Domain = "D" } }),
                     Task.FromResult<IEnumerable<Skill>>(new[] { new Skill { Id = prereq, Name = "Prereq" } }));

        var sut = new GetQuestSkillsQueryHandler(questRepo, subjectRepo, mappingRepo, skillRepo, depRepo);
        var res = await sut.Handle(new GetQuestSkillsQuery { QuestId = q.Id }, CancellationToken.None);

        res!.Skills.Should().HaveCount(1);
        res.Skills![0].Prerequisites!.Should().ContainSingle(p => p.SkillId == prereq && p.SkillName == "Prereq");
    }
}
