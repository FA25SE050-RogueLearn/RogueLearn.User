using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestProgress;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetQuestProgress;

public class GetQuestProgressQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoAttempt_ThrowsNotFound()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var studentRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var diffResolver = Substitute.For<IQuestDifficultyResolver>();

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var sut = new GetQuestProgressQueryHandler(attemptRepo, questRepo, stepRepo, progressRepo, studentRepo, diffResolver);
        var act = () => sut.Handle(new GetQuestProgressQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid() }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_FiltersByCalculatedDifficulty_ComputesLockingAndCounts()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var studentRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var diffResolver = Substitute.For<IQuestDifficultyResolver>();

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);

        // Mock quest and difficulty resolution
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });

        // Fix: Use object initializer
        diffResolver.ResolveDifficulty(Arg.Any<StudentSemesterSubject>())
            .Returns(new QuestDifficultyInfo
            {
                ExpectedDifficulty = "Supportive",
                DifficultyReason = "Reason",
                SubjectStatus = "Status"
            });

        var st1 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, Title = "S1", DifficultyVariant = "Supportive", Content = new Dictionary<string, object> { ["activities"] = new List<object> { new Dictionary<string, object> { ["activityId"] = Guid.NewGuid().ToString() } } } };
        var st2 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 2, Title = "S2", DifficultyVariant = "Supportive", Content = new Dictionary<string, object> { ["activities"] = new List<object> { new Dictionary<string, object> { ["activityId"] = Guid.NewGuid().ToString() }, new Dictionary<string, object> { ["activityId"] = Guid.NewGuid().ToString() } } } };
        var st3 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 3, Title = "S3", DifficultyVariant = "Standard" };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new[] { st3, st2, st1 });

        var p1 = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = st1.Id, Status = StepCompletionStatus.Completed, CompletedActivityIds = new[] { Guid.NewGuid() } };
        var p2 = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = st2.Id, Status = StepCompletionStatus.InProgress, CompletedActivityIds = new[] { Guid.NewGuid() } };
        progressRepo.GetByAttemptIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(new[] { p1, p2 });

        var sut = new GetQuestProgressQueryHandler(attemptRepo, questRepo, stepRepo, progressRepo, studentRepo, diffResolver);
        var res = await sut.Handle(new GetQuestProgressQuery { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        res.Should().HaveCount(2);
        res[0].Title.Should().Be("S1");
        res[0].DifficultyVariant.Should().Be("Supportive");
        res[0].CompletedActivitiesCount.Should().Be(1);
        res[1].Title.Should().Be("S2");
    }
}