using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.SkillDependencies.Commands.AddSkillDependency;

public class AddSkillDependencyCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Create_Dependency_When_Not_Exists()
    {
        var repo = Substitute.For<ISkillDependencyRepository>();
        var logger = Substitute.For<ILogger<AddSkillDependencyCommandHandler>>();
        var handler = new AddSkillDependencyCommandHandler(repo, logger);

        var skillId = Guid.NewGuid();
        var prereqId = Guid.NewGuid();

        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SkillDependency, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var created = new SkillDependency
        {
            Id = Guid.NewGuid(),
            SkillId = skillId,
            PrerequisiteSkillId = prereqId,
            RelationshipType = SkillRelationshipType.Prerequisite,
            CreatedAt = DateTimeOffset.UtcNow
        };
        repo.AddAsync(Arg.Any<SkillDependency>(), Arg.Any<CancellationToken>())
            .Returns(created);

        var cmd = new AddSkillDependencyCommand { SkillId = skillId, PrerequisiteSkillId = prereqId };
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Id.Should().Be(created.Id);
        result.SkillId.Should().Be(skillId);
        result.PrerequisiteSkillId.Should().Be(prereqId);
        result.RelationshipType.Should().Be(SkillRelationshipType.Prerequisite);
    }

    [Fact]
    public async Task Handle_Should_Throw_Conflict_When_Dependency_Exists()
    {
        var repo = Substitute.For<ISkillDependencyRepository>();
        var logger = Substitute.For<ILogger<AddSkillDependencyCommandHandler>>();
        var handler = new AddSkillDependencyCommandHandler(repo, logger);

        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SkillDependency, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var cmd = new AddSkillDependencyCommand { SkillId = Guid.NewGuid(), PrerequisiteSkillId = Guid.NewGuid() };
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_Should_Respect_Provided_RelationshipType()
    {
        var repo = Substitute.For<ISkillDependencyRepository>();
        var logger = Substitute.For<ILogger<AddSkillDependencyCommandHandler>>();
        var handler = new AddSkillDependencyCommandHandler(repo, logger);

        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SkillDependency, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var created = new SkillDependency
        {
            Id = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            PrerequisiteSkillId = Guid.NewGuid(),
            RelationshipType = SkillRelationshipType.Recommended,
            CreatedAt = DateTimeOffset.UtcNow
        };
        repo.AddAsync(Arg.Any<SkillDependency>(), Arg.Any<CancellationToken>())
            .Returns(created);

        var cmd = new AddSkillDependencyCommand { SkillId = created.SkillId, PrerequisiteSkillId = created.PrerequisiteSkillId, RelationshipType = SkillRelationshipType.Recommended };
        var result = await handler.Handle(cmd, CancellationToken.None);
        result.RelationshipType.Should().Be(SkillRelationshipType.Recommended);
    }
}