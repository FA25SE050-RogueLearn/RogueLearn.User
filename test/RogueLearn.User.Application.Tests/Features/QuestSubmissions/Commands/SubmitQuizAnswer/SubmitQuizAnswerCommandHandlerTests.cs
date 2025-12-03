using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitQuizAnswer;
using RogueLearn.User.Application.Features.QuestSubmissions.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.QuestSubmissions.Commands.SubmitQuizAnswer;

public class SubmitQuizAnswerCommandHandlerTests
{
    private static SubmitQuizAnswerCommand CreateBaseCommand()
    {
        return new SubmitQuizAnswerCommand
        {
            AuthUserId = Guid.NewGuid(),
            QuestId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActivityId = Guid.NewGuid(),
            Answers = new Dictionary<string, string> { { "a", "b" } },
            CorrectAnswerCount = 7,
            TotalQuestions = 10
        };
    }

    [Fact]
    public async Task Handle_QuestNotFound_Throws()
    {
        var cmd = CreateBaseCommand();
        var subRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizSvc = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<ILogger<SubmitQuizAnswerCommandHandler>>();

        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns((Quest?)null);

        var sut = new SubmitQuizAnswerCommandHandler(subRepo, stepRepo, questRepo, attemptRepo, quizSvc, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_StepNotFound_Throws()
    {
        var cmd = CreateBaseCommand();
        var subRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizSvc = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<ILogger<SubmitQuizAnswerCommandHandler>>();

        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = cmd.QuestId });
        stepRepo.GetByIdAsync(cmd.StepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = cmd.StepId, QuestId = Guid.NewGuid(), Content = new { } });

        var sut = new SubmitQuizAnswerCommandHandler(subRepo, stepRepo, questRepo, attemptRepo, quizSvc, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActivityUnknown_Throws()
    {
        var cmd = CreateBaseCommand();
        var subRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizSvc = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<ILogger<SubmitQuizAnswerCommandHandler>>();

        var content = new Dictionary<string, object> { { "activities", new List<object>() } };
        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = cmd.QuestId });
        stepRepo.GetByIdAsync(cmd.StepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = cmd.StepId, QuestId = cmd.QuestId, Content = content });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);
        attemptRepo.AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var a = ci.Arg<UserQuestAttempt>();
            a.Id = Guid.NewGuid();
            return a;
        });

        var sut = new SubmitQuizAnswerCommandHandler(subRepo, stepRepo, questRepo, attemptRepo, quizSvc, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_GradesAndStores()
    {
        var cmd = CreateBaseCommand();
        var subRepo = Substitute.For<IQuestSubmissionRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var quizSvc = Substitute.For<IQuizValidationService>();
        var logger = Substitute.For<ILogger<SubmitQuizAnswerCommandHandler>>();

        var activityId = cmd.ActivityId;
        var activities = new List<object>
        {
            new Dictionary<string, object> { { "activityId", activityId.ToString() }, { "type", "Quiz" } }
        };
        var content = new Dictionary<string, object> { { "activities", activities } };

        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = cmd.QuestId });
        stepRepo.GetByIdAsync(cmd.StepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = cmd.StepId, QuestId = cmd.QuestId, Content = content });
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = cmd.AuthUserId, QuestId = cmd.QuestId, Status = QuestAttemptStatus.InProgress });
        quizSvc.EvaluateQuizSubmission(cmd.CorrectAnswerCount, cmd.TotalQuestions).Returns((true, 70m));
        subRepo.AddAsync(Arg.Any<QuestSubmission>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestSubmission>());

        var sut = new SubmitQuizAnswerCommandHandler(subRepo, stepRepo, questRepo, attemptRepo, quizSvc, logger);
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.IsPassed.Should().BeTrue();
        res.ScorePercentage.Should().Be(70);
        await subRepo.Received(1).AddAsync(Arg.Is<QuestSubmission>(s => s.AttemptId != Guid.Empty && s.ActivityId == activityId), Arg.Any<CancellationToken>());
    }
}