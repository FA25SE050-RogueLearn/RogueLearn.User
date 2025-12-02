using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Skills.Commands.DeleteSkill;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Skills.Commands.DeleteSkill;

public class DeleteSkillCommandHandlerTests
{
    private static DeleteSkillCommandHandler CreateSut(ISkillRepository? repo = null, ILogger<DeleteSkillCommandHandler>? logger = null)
    {
        repo ??= Substitute.For<ISkillRepository>();
        logger ??= Substitute.For<ILogger<DeleteSkillCommandHandler>>();
        return new DeleteSkillCommandHandler(repo, logger);
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_When_NotExists()
    {
        var repo = Substitute.For<ISkillRepository>();
        repo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut(repo);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new DeleteSkillCommand { Id = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Deletes_When_Exists()
    {
        var id = Guid.NewGuid();
        var repo = Substitute.For<ISkillRepository>();
        repo.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateSut(repo);
        await sut.Handle(new DeleteSkillCommand { Id = id }, CancellationToken.None);
        await repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}