using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;
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
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
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
}