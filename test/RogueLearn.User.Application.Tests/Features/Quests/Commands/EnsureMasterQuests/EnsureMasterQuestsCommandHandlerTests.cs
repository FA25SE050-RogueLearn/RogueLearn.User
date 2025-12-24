using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Commands.EnsureMasterQuests;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.EnsureMasterQuests;

public class EnsureMasterQuestsCommandHandlerTests
{
    private static EnsureMasterQuestsCommandHandler CreateSut(
        ISubjectRepository? subjectRepo = null,
        IQuestRepository? questRepo = null,
        ILogger<EnsureMasterQuestsCommandHandler>? logger = null)
    {
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        questRepo ??= Substitute.For<IQuestRepository>();
        logger ??= Substitute.For<ILogger<EnsureMasterQuestsCommandHandler>>();
        return new EnsureMasterQuestsCommandHandler(subjectRepo, questRepo, logger);
    }

    [Fact]
    public async Task Handle_AllSubjectsAlreadyHaveQuests_NoCreation()
    {
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var sut = CreateSut(subjectRepo, questRepo);

        var s1 = new Subject { Id = Guid.NewGuid(), SubjectCode = "MTH101", SubjectName = "Math" };
        var s2 = new Subject { Id = Guid.NewGuid(), SubjectCode = "PHY101", SubjectName = "Physics" };
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { s1, s2 });

        var q1 = new Quest { Id = Guid.NewGuid(), SubjectId = s1.Id };
        var q2 = new Quest { Id = Guid.NewGuid(), SubjectId = s2.Id };
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Quest> { q1, q2 });

        var result = await sut.Handle(new EnsureMasterQuestsCommand(), CancellationToken.None);

        result.CreatedCount.Should().Be(0);
        result.ExistingCount.Should().Be(2);
        await questRepo.DidNotReceive().AddAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreatesMissingMasterQuest_ForSubjectsWithoutQuest()
    {
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var sut = CreateSut(subjectRepo, questRepo);

        var s1 = new Subject { Id = Guid.NewGuid(), SubjectCode = "MTH101", SubjectName = "Math", Description = "Desc" };
        var s2 = new Subject { Id = Guid.NewGuid(), SubjectCode = "PHY101", SubjectName = "Physics" };
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { s1, s2 });

        var q1 = new Quest { Id = Guid.NewGuid(), SubjectId = s1.Id };
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Quest> { q1 });

        var result = await sut.Handle(new EnsureMasterQuestsCommand(), CancellationToken.None);

        result.CreatedCount.Should().Be(1);
        result.ExistingCount.Should().Be(1);
        await questRepo.Received(1).AddAsync(Arg.Is<Quest>(q =>
            q.SubjectId == s2.Id &&
            q.Title.StartsWith(s2.SubjectCode) &&
            q.IsActive == true &&
            q.QuestType == QuestType.Practice), Arg.Any<CancellationToken>());
    }

    [Fact]

    public async Task Handle_NoSubjects_NoCreation()
    {
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var sut = CreateSut(subjectRepo, questRepo);

        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Quest>());

        var result = await sut.Handle(new EnsureMasterQuestsCommand(), CancellationToken.None);

        result.CreatedCount.Should().Be(0);
        result.ExistingCount.Should().Be(0);
        await questRepo.DidNotReceive().AddAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>());
    }
}
