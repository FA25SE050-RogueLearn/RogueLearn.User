using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Achievements.Commands.DeleteAchievement;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.DeleteAchievement;

public class DeleteAchievementCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(DeleteAchievementCommand cmd)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var logger = Substitute.For<ILogger<DeleteAchievementCommandHandler>>();

        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((Achievement?)null);

        var sut = new DeleteAchievementCommandHandler(repo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Deletes(DeleteAchievementCommand cmd)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var logger = Substitute.For<ILogger<DeleteAchievementCommandHandler>>();

        var existing = new Achievement { Id = cmd.Id, Key = "k", Name = "n" };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new DeleteAchievementCommandHandler(repo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await repo.Received(1).DeleteAsync(cmd.Id, Arg.Any<CancellationToken>());
    }
}