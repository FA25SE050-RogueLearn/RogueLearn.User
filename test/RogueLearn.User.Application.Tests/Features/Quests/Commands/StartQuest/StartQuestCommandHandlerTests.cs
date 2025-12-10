using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.StartQuest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.StartQuest;

public class StartQuestCommandHandlerTests
{
    private StartQuestCommandHandler CreateSut(
        IUserQuestAttemptRepository attemptRepo = null,
        IQuestRepository questRepo = null)
    {
        attemptRepo ??= Substitute.For<IUserQuestAttemptRepository>();
        questRepo ??= Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<StartQuestCommandHandler>>();

        return new StartQuestCommandHandler(attemptRepo, questRepo, logger);
    }

    [Theory, AutoData]
    public async Task Handle_FirstTimeStart_CreatesNewAttempt(StartQuestCommand cmd)
    {
        // Arrange
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();

        // Quest exists
        var quest = new Quest { Id = cmd.QuestId, ExpectedDifficulty = "Challenging" };
        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns(quest);

        // No existing attempt
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((UserQuestAttempt)null);

        // Capture the created entity to verify
        UserQuestAttempt createdAttempt = null;
        attemptRepo.AddAsync(Arg.Do<UserQuestAttempt>(a => createdAttempt = a), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<UserQuestAttempt>());

        var sut = CreateSut(attemptRepo, questRepo);

        // Act
        // Corresponds to Excel Condition: New user -> Normal
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsNew.Should().BeTrue();
        result.AssignedDifficulty.Should().Be("Challenging"); // Should inherit from Quest
        createdAttempt.Should().NotBeNull();
        createdAttempt.AuthUserId.Should().Be(cmd.AuthUserId);
    }

    [Theory, AutoData]
    public async Task Handle_ExistingAttempt_ReturnsItIdempotently(StartQuestCommand cmd)
    {
        // Arrange
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();

        var quest = new Quest { Id = cmd.QuestId };
        questRepo.GetByIdAsync(cmd.QuestId, Arg.Any<CancellationToken>()).Returns(quest);

        // Existing attempt found
        var existingAttempt = new UserQuestAttempt
        {
            Id = Guid.NewGuid(),
            AuthUserId = cmd.AuthUserId,
            QuestId = cmd.QuestId,
            AssignedDifficulty = "Standard"
        };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(existingAttempt);

        var sut = CreateSut(attemptRepo, questRepo);

        // Act
        // Corresponds to Excel Condition: Existing user -> Boundary/Idempotent
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsNew.Should().BeFalse();
        result.AttemptId.Should().Be(existingAttempt.Id);
        result.AssignedDifficulty.Should().Be("Standard"); // Should keep existing difficulty, NOT overwrite
        await attemptRepo.DidNotReceive().AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>());
    }
}