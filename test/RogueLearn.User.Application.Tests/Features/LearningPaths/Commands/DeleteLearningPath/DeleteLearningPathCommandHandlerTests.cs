using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.LearningPaths.Commands.DeleteLearningPath;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.LearningPaths.Commands.DeleteLearningPath;

public class DeleteLearningPathCommandHandlerTests
{
    [Fact]
    public async Task Handle_DeletesWhenExists()
    {
        var repo = Substitute.For<ILearningPathRepository>();
        var logger = Substitute.For<ILogger<DeleteLearningPathCommandHandler>>();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new LearningPath { Id = id, Name = "LP" });

        var sut = new DeleteLearningPathCommandHandler(repo, logger);
        await sut.Handle(new DeleteLearningPathCommand { Id = id }, CancellationToken.None);

        await repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenMissing()
    {
        var repo = Substitute.For<ILearningPathRepository>();
        var logger = Substitute.For<ILogger<DeleteLearningPathCommandHandler>>();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((LearningPath?)null);

        var sut = new DeleteLearningPathCommandHandler(repo, logger);
        var act = () => sut.Handle(new DeleteLearningPathCommand { Id = id }, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}