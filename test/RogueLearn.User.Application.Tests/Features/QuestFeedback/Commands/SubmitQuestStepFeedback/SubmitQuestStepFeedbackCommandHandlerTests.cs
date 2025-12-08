using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.QuestFeedback.Commands.SubmitQuestStepFeedback;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.QuestFeedback.Commands.SubmitQuestStepFeedback;

public class SubmitQuestStepFeedbackCommandHandlerTests
{
    [Fact]
    public async Task Handle_StepNotFound_Throws()
    {
        var feedbackRepo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<SubmitQuestStepFeedbackCommandHandler>>();
        var cmd = new SubmitQuestStepFeedbackCommand { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid(), Rating = 4, Category = "topic", Comment = "feedback" };
        stepRepo.GetByIdAsync(cmd.StepId, Arg.Any<CancellationToken>()).Returns((QuestStep?)null);

        var sut = new SubmitQuestStepFeedbackCommandHandler(feedbackRepo, stepRepo, questRepo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_StepNotBelongToQuest_Throws()
    {
        var feedbackRepo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<SubmitQuestStepFeedbackCommandHandler>>();
        var cmd = new SubmitQuestStepFeedbackCommand { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid(), Rating = 4, Category = "topic", Comment = "feedback" };
        stepRepo.GetByIdAsync(cmd.StepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = cmd.StepId, QuestId = Guid.NewGuid() });

        var sut = new SubmitQuestStepFeedbackCommandHandler(feedbackRepo, stepRepo, questRepo, logger);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_QuestNotFound_Throws()
    {
        var feedbackRepo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<SubmitQuestStepFeedbackCommandHandler>>();
        var cmd = new SubmitQuestStepFeedbackCommand { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid(), Rating = 4, Category = "topic", Comment = "feedback" };
        stepRepo.GetByIdAsync(cmd.StepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = cmd.StepId, QuestId = cmd.QuestId });
        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns((Quest?)null);

        var sut = new SubmitQuestStepFeedbackCommandHandler(feedbackRepo, stepRepo, questRepo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MissingSubject_Throws()
    {
        var feedbackRepo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<SubmitQuestStepFeedbackCommandHandler>>();
        var cmd = new SubmitQuestStepFeedbackCommand { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid(), Rating = 4, Category = "topic", Comment = "feedback" };
        stepRepo.GetByIdAsync(cmd.StepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = cmd.StepId, QuestId = cmd.QuestId });
        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = cmd.QuestId, SubjectId = null });

        var sut = new SubmitQuestStepFeedbackCommandHandler(feedbackRepo, stepRepo, questRepo, logger);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ReturnsCreatedId()
    {
        var feedbackRepo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<SubmitQuestStepFeedbackCommandHandler>>();
        var cmd = new SubmitQuestStepFeedbackCommand { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid(), Rating = 4, Category = "topic", Comment = "feedback" };
        stepRepo.GetByIdAsync(cmd.StepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = cmd.StepId, QuestId = cmd.QuestId });
        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns(new Quest { Id = cmd.QuestId, SubjectId = Guid.NewGuid() });
        feedbackRepo.AddAsync(Arg.Any<UserQuestStepFeedback>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var f = ci.Arg<UserQuestStepFeedback>();
            f.Id = Guid.NewGuid();
            return f;
        });

        var sut = new SubmitQuestStepFeedbackCommandHandler(feedbackRepo, stepRepo, questRepo, logger);
        var id = await sut.Handle(cmd, CancellationToken.None);
        id.Should().NotBe(Guid.Empty);
        await feedbackRepo.Received(1).AddAsync(Arg.Any<UserQuestStepFeedback>(), Arg.Any<CancellationToken>());
    }
}