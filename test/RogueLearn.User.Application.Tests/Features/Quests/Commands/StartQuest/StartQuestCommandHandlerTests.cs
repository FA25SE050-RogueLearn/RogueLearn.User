using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.StartQuest;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.StartQuest;

public class StartQuestCommandHandlerTests
{
    private static StartQuestCommandHandler CreateSut(
        IUserQuestAttemptRepository? attemptRepo = null,
        IQuestRepository? questRepo = null,
        IStudentSemesterSubjectRepository? studentRepo = null,
        IQuestDifficultyResolver? difficultyResolver = null,
        ILogger<StartQuestCommandHandler>? logger = null)
    {
        attemptRepo ??= Substitute.For<IUserQuestAttemptRepository>();
        questRepo ??= Substitute.For<IQuestRepository>();
        studentRepo ??= Substitute.For<IStudentSemesterSubjectRepository>();
        difficultyResolver ??= Substitute.For<IQuestDifficultyResolver>();
        logger ??= Substitute.For<ILogger<StartQuestCommandHandler>>();
        return new StartQuestCommandHandler(attemptRepo, questRepo, studentRepo, difficultyResolver, logger);
    }

    [Fact]
    public async Task Handle_QuestNotFound_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var sut = CreateSut(attemptRepo: attemptRepo, questRepo: questRepo);

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns((Quest?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new StartQuestCommand(questId, authId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AttemptExists_ReturnsExisting_WithDynamicDifficulty()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var studentRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var difficultyResolver = Substitute.For<IQuestDifficultyResolver>();

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var quest = new Quest { Id = questId, SubjectId = Guid.NewGuid() };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        var existing = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId, Status = QuestAttemptStatus.InProgress };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(existing);

        // Fix: Use object initializer instead of tuple
        difficultyResolver.ResolveDifficulty(Arg.Any<StudentSemesterSubject>())
            .Returns(new QuestDifficultyInfo
            {
                ExpectedDifficulty = "Challenging",
                DifficultyReason = "Reason",
                SubjectStatus = "Passed"
            });

        var sut = CreateSut(attemptRepo, questRepo, studentRepo, difficultyResolver);

        var result = await sut.Handle(new StartQuestCommand(questId, authId), CancellationToken.None);

        result.IsNew.Should().BeFalse();
        result.AttemptId.Should().Be(existing.Id);
        result.Status.Should().Be(existing.Status.ToString());
        result.AssignedDifficulty.Should().Be("Challenging"); // Returned dynamically
        await attemptRepo.DidNotReceive().AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreatesAttempt_Successfully_WithCalculatedDifficulty()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var studentRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var difficultyResolver = Substitute.For<IQuestDifficultyResolver>();

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var quest = new Quest { Id = questId, ExpectedDifficulty = "Supportive" };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);
        attemptRepo.AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestAttempt>());

        // Fix: Use object initializer instead of tuple
        difficultyResolver.ResolveDifficulty(Arg.Any<StudentSemesterSubject>())
            .Returns(new QuestDifficultyInfo
            {
                ExpectedDifficulty = "Supportive",
                DifficultyReason = "Reason",
                SubjectStatus = "NotStarted"
            });

        var sut = CreateSut(attemptRepo, questRepo, studentRepo, difficultyResolver);

        var result = await sut.Handle(new StartQuestCommand(questId, authId), CancellationToken.None);

        result.IsNew.Should().BeTrue();
        result.Status.Should().Be(QuestAttemptStatus.InProgress.ToString());
        result.AssignedDifficulty.Should().Be("Supportive");

        // Ensure attempt is added without AssignedDifficulty property being set (as it doesn't exist)
        // But Notes are set
        await attemptRepo.Received(1).AddAsync(Arg.Is<UserQuestAttempt>(a =>
            a.AuthUserId == authId &&
            a.QuestId == questId &&
            a.Notes!.Contains("Supportive")
        ), Arg.Any<CancellationToken>());
    }
}