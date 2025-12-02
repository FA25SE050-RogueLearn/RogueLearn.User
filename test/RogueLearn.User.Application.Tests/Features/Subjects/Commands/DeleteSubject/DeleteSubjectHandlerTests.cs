using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.DeleteSubject;

public class DeleteSubjectHandlerTests
{
    [Fact]
    public async Task Handle_Should_Delete_When_Subject_Exists()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<DeleteSubjectHandler>>();
        var handler = new DeleteSubjectHandler(repo, logger);

        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Subject { Id = id });

        var cmd = new DeleteSubjectCommand { Id = id };
        await handler.Handle(cmd, CancellationToken.None);

        await repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_Subject_Missing()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<ILogger<DeleteSubjectHandler>>();
        var handler = new DeleteSubjectHandler(repo, logger);

        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Subject?)null);

        var cmd = new DeleteSubjectCommand { Id = id };
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}