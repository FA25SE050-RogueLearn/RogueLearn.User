using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Application.Services;
using Xunit;

namespace RogueLearn.User.Application.Tests.Services;

public class QuestStepGenerationServiceTests
{
    [Fact]
    public async Task GenerateQuestStepsAsync_SendsMediatorCommand_WhenSuccessful()
    {
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuestStepGenerationService>>();
        var sut = new QuestStepGenerationService(mediator, logger);

        var authUserId = Guid.NewGuid();
        var questId = Guid.NewGuid();

        mediator.Send(Arg.Any<GenerateQuestStepsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new List<GeneratedQuestStepDto> { new() });

        await sut.GenerateQuestStepsAsync(authUserId, questId, context: null!);

        await mediator.Received(1).Send(Arg.Is<GenerateQuestStepsCommand>(c => c.AuthUserId == authUserId && c.QuestId == questId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateQuestStepsAsync_Throws_OnErrors()
    {
        var cases = new (Func<Exception> exFactory, Type expectedType)[]
        {
            (new Func<Exception>(() => new BadRequestException("invalid")), typeof(BadRequestException)),
            (new Func<Exception>(() => new NotFoundException("Quest not found")), typeof(NotFoundException))
        };

        foreach (var (exFactory, expectedType) in cases)
        {
            var mediator = Substitute.For<IMediator>();
            var logger = Substitute.For<ILogger<QuestStepGenerationService>>();
            var sut = new QuestStepGenerationService(mediator, logger);

            mediator.Send(Arg.Any<GenerateQuestStepsCommand>(), Arg.Any<CancellationToken>())
                .Returns<Task<List<GeneratedQuestStepDto>>>(_ => throw exFactory());

            var authUserId = Guid.NewGuid();
            var questId = Guid.NewGuid();

            var act = () => sut.GenerateQuestStepsAsync(authUserId, questId, context: null!);
            var assertion = await act.Should().ThrowAsync<Exception>();
            assertion.Which.GetType().Should().Be(expectedType);
        }
    }
}