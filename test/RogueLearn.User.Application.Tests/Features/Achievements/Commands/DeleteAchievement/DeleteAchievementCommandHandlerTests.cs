using System;
using System.Threading;
using System.Threading.Tasks;
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
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var logger = Substitute.For<ILogger<DeleteAchievementCommandHandler>>();

        var cmd = new DeleteAchievementCommand { Id = Guid.NewGuid() };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((Achievement?)null);

        var sut = new DeleteAchievementCommandHandler(repo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Deletes()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var logger = Substitute.For<ILogger<DeleteAchievementCommandHandler>>();

        var id = Guid.NewGuid();
        var cmd = new DeleteAchievementCommand { Id = id };
        var existing = new Achievement { Id = id, Key = "k", Name = "n" };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new DeleteAchievementCommandHandler(repo, logger);
        await sut.Handle(cmd, CancellationToken.None);

        await repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}