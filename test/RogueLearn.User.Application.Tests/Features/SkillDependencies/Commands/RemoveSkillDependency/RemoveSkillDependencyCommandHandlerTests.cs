using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.RemoveSkillDependency;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.SkillDependencies.Commands.RemoveSkillDependency;

public class RemoveSkillDependencyCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Remove_Dependency_When_Found()
    {
        var repo = Substitute.For<ISkillDependencyRepository>();
        var logger = Substitute.For<ILogger<RemoveSkillDependencyCommandHandler>>();
        var handler = new RemoveSkillDependencyCommandHandler(repo, logger);

        var skillId = Guid.NewGuid();
        var prereqId = Guid.NewGuid();
        var dep = new SkillDependency { Id = Guid.NewGuid(), SkillId = skillId, PrerequisiteSkillId = prereqId };
        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SkillDependency, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(dep);

        var cmd = new RemoveSkillDependencyCommand { SkillId = skillId, PrerequisiteSkillId = prereqId };
        await handler.Handle(cmd, CancellationToken.None);

        await repo.Received(1).DeleteAsync(dep.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_Dependency_Missing()
    {
        var repo = Substitute.For<ISkillDependencyRepository>();
        var logger = Substitute.For<ILogger<RemoveSkillDependencyCommandHandler>>();
        var handler = new RemoveSkillDependencyCommandHandler(repo, logger);

        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SkillDependency, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((SkillDependency?)null);

        var cmd = new RemoveSkillDependencyCommand { SkillId = Guid.NewGuid(), PrerequisiteSkillId = Guid.NewGuid() };
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}