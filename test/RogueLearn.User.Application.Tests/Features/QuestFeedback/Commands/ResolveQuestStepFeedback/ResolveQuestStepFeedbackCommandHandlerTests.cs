using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.QuestFeedback.Commands.ResolveQuestStepFeedback;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.QuestFeedback.Commands.ResolveQuestStepFeedback;

public class ResolveQuestStepFeedbackCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(ResolveQuestStepFeedbackCommand cmd)
    {
        var repo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ResolveQuestStepFeedbackCommandHandler>>();

        repo.GetByIdAsync(cmd.FeedbackId, Arg.Any<CancellationToken>()).Returns((UserQuestStepFeedback?)null);

        var sut = new ResolveQuestStepFeedbackCommandHandler(repo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Updates(ResolveQuestStepFeedbackCommand cmd)
    {
        var repo = Substitute.For<IUserQuestStepFeedbackRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ResolveQuestStepFeedbackCommandHandler>>();

        var fb = new UserQuestStepFeedback { Id = cmd.FeedbackId, IsResolved = false };
        repo.GetByIdAsync(cmd.FeedbackId, Arg.Any<CancellationToken>()).Returns(fb);

        var sut = new ResolveQuestStepFeedbackCommandHandler(repo, logger);
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<UserQuestStepFeedback>(f => f.IsResolved == cmd.IsResolved && f.AdminNotes == cmd.AdminNotes), Arg.Any<CancellationToken>());
    }
}