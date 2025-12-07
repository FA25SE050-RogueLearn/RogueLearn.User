using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.QuestFeedback.Commands.ResolveQuestStepFeedback;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.QuestFeedback.Commands.ResolveQuestStepFeedback;

public class ResolveQuestStepFeedbackCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ResolveQuestStepFeedbackCommandHandler>>();

        var cmd = new ResolveQuestStepFeedbackCommand { FeedbackId = Guid.NewGuid(), IsResolved = true, AdminNotes = "notes" };
        repo.GetByIdAsync(cmd.FeedbackId, Arg.Any<CancellationToken>()).Returns((UserQuestStepFeedback?)null);

        var sut = new ResolveQuestStepFeedbackCommandHandler(repo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Updates()
    {
        var repo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ResolveQuestStepFeedbackCommandHandler>>();

        var cmd = new ResolveQuestStepFeedbackCommand { FeedbackId = Guid.NewGuid(), IsResolved = true, AdminNotes = "done" };
        var fb = new UserQuestStepFeedback { Id = cmd.FeedbackId, IsResolved = false };
        repo.GetByIdAsync(cmd.FeedbackId, Arg.Any<CancellationToken>()).Returns(fb);

        var sut = new ResolveQuestStepFeedbackCommandHandler(repo, logger);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<UserQuestStepFeedback>(f => f.IsResolved == cmd.IsResolved && f.AdminNotes == cmd.AdminNotes), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_UpdatesFeedback()
    {
        var repo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ResolveQuestStepFeedbackCommandHandler>>();

        var feedbackId = Guid.NewGuid();
        var feedback = new UserQuestStepFeedback { Id = feedbackId, IsResolved = false, AdminNotes = null };
        repo.GetByIdAsync(feedbackId, Arg.Any<CancellationToken>()).Returns(feedback);

        var cmd = new ResolveQuestStepFeedbackCommand { FeedbackId = feedbackId, IsResolved = true, AdminNotes = "notes" };
        var sut = new ResolveQuestStepFeedbackCommandHandler(repo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await repo.Received(1).UpdateAsync(Arg.Is<UserQuestStepFeedback>(f => f.IsResolved == true && f.AdminNotes == "notes"), Arg.Any<CancellationToken>());
    }
}
