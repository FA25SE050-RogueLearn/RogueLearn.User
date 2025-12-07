using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;
using RogueLearn.User.Application.Features.Quests.Services;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommandHandlerTests
{
    [Fact]
    public async Task Handle_ThrowsNotFoundWhenStepMissing()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new RogueLearn.User.Application.Features.Quests.Services.ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<RogueLearn.User.Application.Features.Quests.Services.ActivityValidationService>>());

        stepRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((QuestStep?)null);
        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, Substitute.For<MediatR.IMediator>(), Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = Guid.NewGuid(), StepId = Guid.NewGuid(), ActivityId = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Status = StepCompletionStatus.InProgress }, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => act());
    }

    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "Quiz",
                        ["payload"] = new Dictionary<string, object> { ["skillId"] = Guid.NewGuid().ToString(), ["experiencePoints"] = 10 }
                    }
                }
            }
        };

        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.NotStarted });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>())
                   .Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        var stepProgress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>())
                    .Returns(stepProgress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>())
                      .Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = false, Grade = 4, MaxGrade = 10 });

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, logger);
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand
        {
            QuestId = questId,
            StepId = stepId,
            ActivityId = activityId,
            AuthUserId = authId,
            Status = StepCompletionStatus.Completed
        }, CancellationToken.None);

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ValidationException>(() => act());
    }

    [Fact]
    public async Task Handle_IdempotentCompleted_ReturnsEarly_NoXpDispatched()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "Reading",
                        ["payload"] = new Dictionary<string, object> { ["experiencePoints"] = 5 }
                    }
                }
            }
        };

        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>())
                   .Returns(attempt);

        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress, CompletedActivityIds = new[] { activityId } };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>())
                    .Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, logger);
        await sut.Handle(new UpdateQuestActivityProgressCommand
        {
            QuestId = questId,
            StepId = stepId,
            ActivityId = activityId,
            AuthUserId = authId,
            Status = StepCompletionStatus.Completed
        }, CancellationToken.None);

        await mediator.DidNotReceive().Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
        await progressRepo.DidNotReceive().UpdateAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesProgressForInProgress()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new RogueLearn.User.Application.Features.Quests.Services.ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<RogueLearn.User.Application.Features.Quests.Services.ActivityValidationService>>());

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var quest = new Quest { Id = questId, Status = QuestStatus.NotStarted };
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = new Dictionary<string, object>() };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        // No global overrides here; we want MarkParentQuestAsInProgress to update when NotStarted

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);
        attemptRepo.AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestAttempt>());

        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);
        progressRepo.AddAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestStepProgress>());
        progressRepo.UpdateAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestStepProgress>());
        questRepo.UpdateAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Quest>());

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, Substitute.For<MediatR.IMediator>(), Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Status = StepCompletionStatus.InProgress }, CancellationToken.None);
        await questRepo.Received(1).UpdateAsync(Arg.Is<Quest>(q => q.Status == QuestStatus.InProgress), Arg.Any<CancellationToken>());
        await progressRepo.Received(1).UpdateAsync(Arg.Is<UserQuestStepProgress>(p => p.Status == StepCompletionStatus.InProgress), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesProgressForCompleted()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new RogueLearn.User.Application.Features.Quests.Services.ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<RogueLearn.User.Application.Features.Quests.Services.ActivityValidationService>>());

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var quest = new Quest { Id = questId, Status = QuestStatus.InProgress };
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = new Dictionary<string, object> { ["activities"] = new List<object> { new Dictionary<string, object> { ["activityId"] = Guid.NewGuid() } } } };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);

        var existingAttempt = new UserQuestAttempt { Id = Guid.NewGuid(), QuestId = questId, AuthUserId = Guid.NewGuid(), Status = QuestAttemptStatus.InProgress };
        var progress = new UserQuestStepProgress { AttemptId = existingAttempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress, CompletedActivityIds = Array.Empty<Guid>() };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(existingAttempt);
        attemptRepo.GetByIdAsync(existingAttempt.Id, Arg.Any<CancellationToken>()).Returns(existingAttempt);
        progressRepo.UpdateAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestStepProgress>());
        questRepo.UpdateAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Quest>());

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, Substitute.For<MediatR.IMediator>(), Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), Status = StepCompletionStatus.Completed }, CancellationToken.None);

        await progressRepo.Received(1).UpdateAsync(Arg.Is<UserQuestStepProgress>(p => p.Status == StepCompletionStatus.Completed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Completed_DispatchesXpAndMarksProgress()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validatorLogger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>();
        var validator = new ActivityValidationService(submissionRepo, validatorLogger);
        var mediator = Substitute.For<MediatR.IMediator>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var quest = new Quest { Id = questId, Status = QuestStatus.NotStarted };
        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Title = "Week 1",
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "quiz",
                        ["payload"] = new Dictionary<string, object>
                        {
                            ["skillId"] = Guid.NewGuid().ToString(),
                            ["experiencePoints"] = 50
                        }
                    }
                }
            }
        };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>())
                   .Returns((UserQuestAttempt?)null);
        attemptRepo.AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var a = ci.Arg<UserQuestAttempt>();
            a.Id = Guid.NewGuid();
            return a;
        });
        attemptRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ci => new UserQuestAttempt { Id = (Guid)ci[0], QuestId = questId, Status = QuestAttemptStatus.InProgress });

        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>())
                    .Returns((UserQuestStepProgress?)null);
        progressRepo.AddAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestStepProgress>());
        progressRepo.UpdateAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestStepProgress>());
        questRepo.UpdateAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Quest>());

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>())
                      .Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 8, MaxGrade = 10 });

        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new IngestXpEventResponse { Processed = true }));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, logger);
        await sut.Handle(new UpdateQuestActivityProgressCommand
        {
            QuestId = questId,
            StepId = stepId,
            ActivityId = activityId,
            AuthUserId = authId,
            Status = StepCompletionStatus.Completed
        }, CancellationToken.None);

        await questRepo.Received(1).UpdateAsync(Arg.Is<Quest>(q => q.Status == QuestStatus.InProgress), Arg.Any<CancellationToken>());
        await mediator.Received(1).Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
        await progressRepo.Received().UpdateAsync(Arg.Is<UserQuestStepProgress>(p => p.CompletedActivityIds!.Contains(activityId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InProgress_RaceConditionOnStepProgressCreation_Recovers()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var quest = new Quest { Id = questId, Status = QuestStatus.NotStarted };
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = new Dictionary<string, object>() };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>())
                  .Returns((UserQuestAttempt?)null);
        attemptRepo.AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestAttempt>());

        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>())
                    .Returns((UserQuestStepProgress?)null,
                             new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = Guid.NewGuid(), StepId = stepId, Status = StepCompletionStatus.InProgress });

        progressRepo.AddAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>())
                    .Returns<Task<UserQuestStepProgress>>(ci => throw new Exception("duplicate key value violates unique constraint"));
        progressRepo.UpdateAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestStepProgress>());
        questRepo.UpdateAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Quest>());

        var sut = new UpdateQuestActivityProgressCommandHandler(
            attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator,
            Substitute.For<MediatR.IMediator>(), Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());

        await sut.Handle(new UpdateQuestActivityProgressCommand
        {
            QuestId = questId,
            StepId = stepId,
            ActivityId = Guid.NewGuid(),
            AuthUserId = Guid.NewGuid(),
            Status = StepCompletionStatus.InProgress
        }, CancellationToken.None);

        await progressRepo.Received(1).UpdateAsync(Arg.Is<UserQuestStepProgress>(p => p.StepId == stepId && p.Status == StepCompletionStatus.InProgress), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Completed_ActivityIdGuidType_IsHandled()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var quest = new Quest { Id = questId, Status = QuestStatus.NotStarted };
        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId,
                        ["type"] = "quiz"
                    }
                }
            }
        };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);

        questRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        questRepo.UpdateAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Quest>());
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>())
                   .Returns((UserQuestAttempt?)null);
        attemptRepo.AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var a = ci.Arg<UserQuestAttempt>();
            a.Id = Guid.NewGuid();
            return a;
        });
        attemptRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ci => new UserQuestAttempt { Id = (Guid)ci[0], QuestId = questId, Status = QuestAttemptStatus.InProgress });

        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>())
                    .Returns((UserQuestStepProgress?)null);
        progressRepo.AddAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestStepProgress>());
        progressRepo.UpdateAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestStepProgress>());
        questRepo.UpdateAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Quest>());

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>())
                      .Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 10, MaxGrade = 10 });

        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new IngestXpEventResponse { Processed = true }));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand
        {
            QuestId = questId,
            StepId = stepId,
            ActivityId = activityId,
            AuthUserId = authId,
            Status = StepCompletionStatus.Completed
        }, CancellationToken.None);

        await progressRepo.Received().UpdateAsync(Arg.Is<UserQuestStepProgress>(p => p.CompletedActivityIds!.Contains(activityId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Completed_AllStepsCompleted_MarksAttemptAndQuestCompleted()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var quest = new Quest { Id = questId, Status = QuestStatus.InProgress };
        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "Quiz",
                        ["payload"] = new Dictionary<string, object> { ["experiencePoints"] = 5 }
                    }
                }
            }
        };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId, Status = QuestAttemptStatus.InProgress };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.UpdateAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestAttempt>());

        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress, CompletedActivityIds = Array.Empty<Guid>() };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);
        progressRepo.UpdateAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestStepProgress>());
        progressRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => new List<UserQuestStepProgress> { new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.Completed } });

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 8, MaxGrade = 10 });
        questRepo.UpdateAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Quest>());

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);

        await attemptRepo.Received(1).UpdateAsync(Arg.Is<UserQuestAttempt>(a => a.Status == QuestAttemptStatus.Completed), Arg.Any<CancellationToken>());
        await questRepo.Received(1).UpdateAsync(Arg.Is<Quest>(q => q.Status == QuestStatus.Completed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Validation_StringJsonContent_FailsForQuiz()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var contentJson = "{\"activities\":[{\"activityId\":\"" + activityId + "\",\"type\":\"Quiz\",\"payload\":{\"experiencePoints\":10}}]}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };

        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = false, Grade = 5, MaxGrade = 10 });

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ValidationException>(() => act());
    }
    
    [Fact]
    public async Task Handle_ContentEmptyString_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep { Id = stepId, QuestId = questId, Content = "" };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act());
    }

    [Fact]
    public async Task Handle_ContentStringActivitiesNotArray_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var contentJson = "{\"activities\":{}}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act2 = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act2());
    }

    [Fact]
    public async Task Handle_ContentDictionaryNoActivities_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var content = new Dictionary<string, object> { ["meta"] = "x" };
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = content };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act());
    }

    [Fact]
    public async Task Handle_ContentUnsupportedType_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep { Id = stepId, QuestId = questId, Content = new object() };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act());
    }

    [Fact]
    public async Task Handle_ActivityNotFound_StillUpdatesProgress()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var contentJson = "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"Quiz\",\"payload\":{\"experiencePoints\":10}}]}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await progressRepo.Received(1).UpdateAsync(Arg.Is<UserQuestStepProgress>(p => p.CompletedActivityIds!.Contains(activityId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PayloadString_DispatchesXp()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var payloadStr = "{\"skillId\":\"" + Guid.NewGuid() + "\",\"experiencePoints\":25}";
        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "Quiz",
                        ["payload"] = payloadStr
                    }
                }
            }
        };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 8, MaxGrade = 10 });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new IngestXpEventResponse { Processed = true }));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await mediator.Received(1).Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PayloadJsonElement_DispatchesXp()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        using var doc = System.Text.Json.JsonDocument.Parse("{\"skillId\":\"" + Guid.NewGuid() + "\",\"experiencePoints\":30}");
        var payloadElement = doc.RootElement;

        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "Quiz",
                        ["payload"] = payloadElement
                    }
                }
            }
        };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 9, MaxGrade = 10 });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new IngestXpEventResponse { Processed = true }));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await mediator.Received(1).Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PayloadMissingKeys_DoesNotDispatch()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "Quiz",
                        ["payload"] = new Dictionary<string, object> { ["experiencePoints"] = 10 }
                    }
                }
            }
        };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 8, MaxGrade = 10 });

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await mediator.DidNotReceive().Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateAsyncNoResults_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "Quiz",
                        ["payload"] = new Dictionary<string, object> { ["skillId"] = Guid.NewGuid().ToString(), ["experiencePoints"] = 10 }
                    }
                }
            }
        };

        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 8, MaxGrade = 10 });
        progressRepo.UpdateAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns<Task<UserQuestStepProgress>>(ci => throw new InvalidOperationException("no results"));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act());
    }

    [Fact]
    public async Task Handle_RaceConditionCritical_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var quest = new Quest { Id = questId, Status = QuestStatus.NotStarted };
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = new Dictionary<string, object>() };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);
        attemptRepo.AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestAttempt>());

        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null, (UserQuestStepProgress?)null);
        progressRepo.AddAsync(Arg.Any<UserQuestStepProgress>(), Arg.Any<CancellationToken>()).Returns<Task<UserQuestStepProgress>>(ci => throw new Exception("duplicate key value violates unique constraint"));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.InProgress }, CancellationToken.None);
        await Assert.ThrowsAsync<Exception>(() => act());
    }

    [Fact]
    public async Task Handle_Completed_UnknownType_DoesNotFail()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var contentJson = "{\"activities\":[{\"activityId\":\"" + activityId + "\",\"type\":null,\"payload\":{\"experiencePoints\":10}}]}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 9, MaxGrade = 10 });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new IngestXpEventResponse { Processed = true }));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ExtractType_ActivitiesArrayEmpty_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var contentJson = "{\"activities\":[]}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act());
    }

    [Fact]
    public async Task Handle_ExtractType_InvalidGuid_SkipsAndCompletes()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var badIdJson = "{\"activityId\":\"not-guid\",\"type\":\"Quiz\"}";
        var goodJson = "{\"activityId\":\"" + activityId + "\",\"type\":\"Quiz\",\"payload\":{\"skillId\":\"" + Guid.NewGuid() + "\",\"experiencePoints\":5}}";
        var contentJson = "{\"activities\":[" + badIdJson + "," + goodJson + "]}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 8, MaxGrade = 10 });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new IngestXpEventResponse { Processed = true }));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await mediator.Received(1).Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExtractType_MissingActivityId_SkipsAndCompletes()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var missingIdJson = "{\"type\":\"Quiz\"}";
        var goodJson = "{\"activityId\":\"" + activityId + "\",\"type\":\"Quiz\",\"payload\":{\"skillId\":\"" + Guid.NewGuid() + "\",\"experiencePoints\":5}}";
        var contentJson = "{\"activities\":[" + missingIdJson + "," + goodJson + "]}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 8, MaxGrade = 10 });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new IngestXpEventResponse { Processed = true }));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await mediator.Received(1).Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ContentJObject_ParsesAndCompletes()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var jObj = Newtonsoft.Json.Linq.JObject.Parse("{\"activities\":[{\"activityId\":\"" + activityId + "\",\"type\":\"Quiz\",\"payload\":{\"skillId\":\"" + Guid.NewGuid() + "\",\"experiencePoints\":15}}]}");
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = jObj };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 8, MaxGrade = 10 });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new IngestXpEventResponse { Processed = true }));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await mediator.Received(1).Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ContentMalformedJson_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var contentJson = "{\"activities\":[{";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act());
    }

    [Fact]
    public async Task Handle_ActivityObjectIsString_Completes()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var actStr = "{\"activityId\":\"" + activityId + "\",\"type\":\"Quiz\",\"payload\":{\"skillId\":\"" + Guid.NewGuid() + "\",\"experiencePoints\":12}}";
        var contentJson = "{\"activities\":[" + actStr + "]}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 8, MaxGrade = 10 });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new IngestXpEventResponse { Processed = true }));

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await mediator.Received(1).Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidSkillId_DoesNotDispatchXp()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep
        {
            Id = stepId,
            QuestId = questId,
            Content = new Dictionary<string, object>
            {
                ["activities"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["activityId"] = activityId.ToString(),
                        ["type"] = "Quiz",
                        ["payload"] = new Dictionary<string, object> { ["skillId"] = "not-guid", ["experiencePoints"] = 10 }
                    }
                }
            }
        };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);
        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 9, MaxGrade = 10 });

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await mediator.DidNotReceive().Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreateAttemptAndMarkQuestInProgress()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var quest = new Quest { Id = questId, Status = QuestStatus.NotStarted };
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = new Dictionary<string, object>() };
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);
        attemptRepo.AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserQuestAttempt>());
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = Guid.NewGuid(), StepId = stepId, Status = StepCompletionStatus.InProgress });

        await new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>())
            .Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.InProgress }, CancellationToken.None);

        await questRepo.Received(1).UpdateAsync(Arg.Is<Quest>(q => q.Status == QuestStatus.InProgress), Arg.Any<CancellationToken>());
        await attemptRepo.Received(1).AddAsync(Arg.Is<UserQuestAttempt>(a => a.AuthUserId == authId && a.QuestId == questId), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Handle_AllStepsCompleted_MarksQuestCompleted()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId1 = Guid.NewGuid();
        var stepId2 = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var quest = new Quest { Id = questId, Status = QuestStatus.InProgress };
        var step1 = new QuestStep { Id = stepId1, QuestId = questId, Content = new Dictionary<string, object> { ["activities"] = new List<object> { new Dictionary<string, object> { ["activityId"] = activityId.ToString(), ["type"] = "Quiz", ["payload"] = new Dictionary<string, object> { ["skillId"] = Guid.NewGuid().ToString(), ["experiencePoints"] = 3 } } } } };
        var step2 = new QuestStep { Id = stepId2, QuestId = questId, Content = new Dictionary<string, object> { ["activities"] = new List<object> { new Dictionary<string, object> { ["activityId"] = Guid.NewGuid().ToString(), ["type"] = "Quiz", ["payload"] = new Dictionary<string, object> { ["skillId"] = Guid.NewGuid().ToString(), ["experiencePoints"] = 2 } } } } };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(quest);
        stepRepo.GetByIdAsync(stepId1, Arg.Any<CancellationToken>()).Returns(step1);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step1, step2 });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        var sp1 = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId1, Status = StepCompletionStatus.Completed };
        var sp2 = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId2, Status = StepCompletionStatus.Completed };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(sp1);
        progressRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<UserQuestStepProgress> { sp1, sp2 });

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 10, MaxGrade = 10 });

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId1, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);

        await questRepo.Received(1).UpdateAsync(Arg.Is<Quest>(q => q.Id == questId && q.Status == QuestStatus.Completed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExtractType_NestedArrays_WarnsAndCompletes()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var inner = "{\"activityId\":\"" + activityId + "\",\"type\":\"Quiz\",\"payload\":{\"skillId\":\"" + Guid.NewGuid() + "\",\"experiencePoints\":7}}";
        var contentJson = "{\"activities\":[[" + inner + "]]}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };

        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        stepRepo.FindByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { step });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        submissionRepo.GetLatestByActivityAndUserAsync(activityId, authId, Arg.Any<CancellationToken>()).Returns(new QuestSubmission { ActivityId = activityId, UserId = authId, IsPassed = true, Grade = 7, MaxGrade = 10 });

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act());
    }

    [Fact]
    public void Handle_ExtractType_RootIsArray_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var contentJson = "[{\"activityId\":\"" + activityId + "\",\"type\":\"Quiz\"}]";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);
    }

    [Fact]
    public async Task Handle_ContentNull_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep { Id = stepId, QuestId = questId, Content = null };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act());
    }

    private class BadToString
    {
        public override string ToString() => throw new Exception("bad toString");
    }

    private class EmptyToString
    {
        public override string ToString() => string.Empty;
    }

    private class JObject
    {
        public override string ToString() => throw new Exception("jobject toString failed");
    }

    [Fact]
    public async Task Handle_ExtractType_UnsupportedTypeToStringEmpty_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep { Id = stepId, QuestId = questId, Content = new EmptyToString() };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => act());
    }

    [Fact]
public async Task Handle_ExtractType_JObjectToStringThrows_Throws()
{
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var step = new QuestStep { Id = stepId, QuestId = questId, Content = new JObject() };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        var act = () => sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        await Assert.ThrowsAsync<Exception>(() => act());
}

    [Fact]
    public async Task Handle_ExtractType_ActivityIdNull_CompletesWithoutXp()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var contentJson = "{\"activities\":[{\"activityId\":null}] }";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        progress.CompletedActivityIds.Should().Contain(activityId);
        await mediator.DidNotReceive().Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExtractType_MissingTypeProperty_CompletesWithoutXp()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var submissionRepo = Substitute.For<IQuestSubmissionRepository>();
        var validator = new ActivityValidationService(submissionRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<ActivityValidationService>>());
        var mediator = Substitute.For<MediatR.IMediator>();

        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var authId = Guid.NewGuid();

        var contentJson = "{\"activities\":[{\"activityId\":\"" + activityId + "\"}]}";
        var step = new QuestStep { Id = stepId, QuestId = questId, Content = contentJson };
        stepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(step);
        questRepo.GetByIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = questId, Status = QuestStatus.InProgress });

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        attemptRepo.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress };
        progressRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new UpdateQuestActivityProgressCommandHandler(attemptRepo, progressRepo, stepRepo, questRepo, submissionRepo, validator, mediator, Substitute.For<Microsoft.Extensions.Logging.ILogger<UpdateQuestActivityProgressCommandHandler>>());
        await sut.Handle(new UpdateQuestActivityProgressCommand { QuestId = questId, StepId = stepId, ActivityId = activityId, AuthUserId = authId, Status = StepCompletionStatus.Completed }, CancellationToken.None);
        progress.CompletedActivityIds.Should().Contain(activityId);
        await mediator.DidNotReceive().Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }
}
