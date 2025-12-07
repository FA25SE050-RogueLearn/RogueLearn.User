using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetUserProgressForQuest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.QuestProgress.Queries.GetUserProgressForQuest;

public class GetUserProgressForQuestQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoAttempt_ReturnsNotStarted()
    {
        var query = new GetUserProgressForQuestQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid() };
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var sut = new GetUserProgressForQuestQueryHandler(attemptRepo, stepRepo);

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var result = await sut.Handle(query, CancellationToken.None);
        result!.QuestId.Should().Be(query.QuestId);
        result.QuestStatus.Should().Be("NotStarted");
        result.StepStatuses.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithAttempt_ReturnsStatuses()
    {
        var query = new GetUserProgressForQuestQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid() };
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var sut = new GetUserProgressForQuestQueryHandler(attemptRepo, stepRepo);

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = query.AuthUserId, QuestId = query.QuestId, Status = QuestAttemptStatus.InProgress };
        var sp = new UserQuestStepProgress { AttemptId = attempt.Id, StepId = Guid.NewGuid(), Status = StepCompletionStatus.Completed };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { sp });

        var result = await sut.Handle(query, CancellationToken.None);
        result!.QuestStatus.Should().Be("InProgress");
        result.StepStatuses[sp.StepId].Should().Be("Completed");
    }
}