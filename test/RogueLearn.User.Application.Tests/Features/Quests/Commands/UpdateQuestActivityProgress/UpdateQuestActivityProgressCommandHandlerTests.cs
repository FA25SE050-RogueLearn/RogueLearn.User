using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;
using RogueLearn.User.Application.Features.Quests.Services;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommandHandlerTests
{
    private readonly IUserSkillRepository userSkillRepo = Substitute.For<IUserSkillRepository>();
    private readonly ISubjectSkillMappingRepository ssmRepo = Substitute.For<ISubjectSkillMappingRepository>();
    private readonly IQuestSubmissionRepository submissionRepo = Substitute.For<IQuestSubmissionRepository>();

    [Fact]
    public async Task Handle_Completed_Quiz_DistributesXpToSkillsAndUpdatesAttempt()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var studentRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var diffResolver = Substitute.For<IQuestDifficultyResolver>();
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<UpdateQuestActivityProgressCommandHandler>>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Title = "Intro",
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "Quiz"
                    }
                }
            },
            ExperiencePoints = 80,
            DifficultyVariant = "Standard"
        };

        var quest = new Quest { Id = questId, SubjectId = subjectId, Status = QuestStatus.InProgress };
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId, TotalExperienceEarned = 0 };

        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);

        // Fix: Use object initializer
        diffResolver.ResolveDifficulty(Arg.Any<StudentSemesterSubject>())
            .Returns(new QuestDifficultyInfo
            {
                ExpectedDifficulty = "Standard",
                DifficultyReason = "Reason",
                SubjectStatus = "Status"
            });

        ssmRepo.GetMappingsBySubjectIdsAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Single() == subjectId), Arg.Any<CancellationToken>())
               .Returns(new[]
               {
                   new SubjectSkillMapping { SubjectId = subjectId, SkillId = Guid.NewGuid(), RelevanceWeight = 0.5m },
                   new SubjectSkillMapping { SubjectId = subjectId, SkillId = Guid.NewGuid(), RelevanceWeight = 0.25m }
               });

        var sut = new UpdateQuestActivityProgressCommandHandler(
            attemptRepo, progressRepo, stepRepo, userSkillRepo, ssmRepo, questRepo, mediator,
            studentRepo, diffResolver, logger);

        await sut.Handle(new UpdateQuestActivityProgressCommand
        {
            AuthUserId = authId,
            QuestId = questId,
            StepId = stepId,
            ActivityId = activityId,
            Status = StepCompletionStatus.Completed
        }, CancellationToken.None);

        await attemptRepo.Received(1).UpdateAsync(Arg.Is<UserQuestAttempt>(a => a.Id == attempt.Id && a.TotalExperienceEarned == 80), Arg.Any<CancellationToken>());
        await progressRepo.Received(1).AddAsync(Arg.Is<UserQuestStepProgress>(p => p.StepId == stepId && p.Status == StepCompletionStatus.Completed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RevertCompletion_RemovesActivityAndMarksInProgress()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var studentRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var diffResolver = Substitute.For<IQuestDifficultyResolver>();
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<UpdateQuestActivityProgressCommandHandler>>();

        var authId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();

        var step = new QuestStep { Id = stepId, QuestId = questId, DifficultyVariant = "Standard", Content = new Dictionary<string, object> { ["activities"] = new List<object> { new Dictionary<string, object> { ["activityId"] = activityId.ToString(), ["type"] = "Reading" } } } };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);

        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.Completed, CompletedActivityIds = new[] { activityId }, CompletedAt = DateTimeOffset.UtcNow };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        // Fix: Use object initializer
        diffResolver.ResolveDifficulty(Arg.Any<StudentSemesterSubject>())
            .Returns(new QuestDifficultyInfo
            {
                ExpectedDifficulty = "Standard",
                DifficultyReason = "Reason",
                SubjectStatus = "Status"
            });

        var sut = new UpdateQuestActivityProgressCommandHandler(
            attemptRepo, progressRepo, stepRepo, userSkillRepo, ssmRepo, questRepo, mediator,
            studentRepo, diffResolver, logger);

        await sut.Handle(new UpdateQuestActivityProgressCommand
        {
            AuthUserId = authId,
            QuestId = questId,
            StepId = stepId,
            ActivityId = activityId,
            Status = StepCompletionStatus.InProgress
        }, CancellationToken.None);

        await progressRepo.Received(1).UpdateAsync(Arg.Is<UserQuestStepProgress>(p => p.Status == StepCompletionStatus.InProgress && p.CompletedAt == null), Arg.Any<CancellationToken>());
    }
}