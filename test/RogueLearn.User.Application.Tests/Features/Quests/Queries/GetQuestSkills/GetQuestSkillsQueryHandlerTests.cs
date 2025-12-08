using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestSkills;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetQuestSkills;

public class GetQuestSkillsQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoMappings_ReturnsResponseWithEmptySkills()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var depRepo = Substitute.For<ISkillDependencyRepository>();

        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, SubjectId = subjectId });
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(new Subject { Id = subjectId, SubjectName = "Math" });
        mappingRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubjectSkillMapping>());

        var sut = new GetQuestSkillsQueryHandler(questRepo, subjectRepo, mappingRepo, skillRepo, depRepo);
        var res = await sut.Handle(new GetQuestSkillsQuery { QuestId = questId }, CancellationToken.None);
        res.Should().NotBeNull();
        res!.SubjectName.Should().Be("Math");
        res.Skills.Should().NotBeNull();
        res.Skills.Count.Should().Be(0);
    }
}

