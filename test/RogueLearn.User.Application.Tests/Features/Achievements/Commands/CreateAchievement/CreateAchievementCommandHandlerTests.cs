using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.CreateAchievement;

public class CreateAchievementCommandHandlerTests
{
    [Fact]
    public async Task Handle_KeyExists_ThrowsConflict()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new CreateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<CreateAchievementCommandHandler>>();

        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateAchievementCommand
        {
            Key = "key",
            Name = "name",
            Description = "desc",
            SourceService = "svc",
            RuleType = null,
            Category = null,
            IconUrl = null,
            RuleConfig = null
        };

        var sut = new CreateAchievementCommandHandler(repo, mapper, validator, logger);
        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidRuleConfigArray_ThrowsValidation()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new CreateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<CreateAchievementCommandHandler>>();

        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateAchievementCommand
        {
            Key = "key",
            Name = "name",
            Description = "desc",
            SourceService = "svc",
            RuleType = null,
            Category = null,
            IconUrl = null,
            RuleConfig = "[]"
        };

        var sut = new CreateAchievementCommandHandler(repo, mapper, validator, logger);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ValidationException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ReturnsResponse()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new CreateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<CreateAchievementCommandHandler>>();

        var command = new CreateAchievementCommand
        {
            Key = "key",
            Name = "name",
            Description = "desc",
            SourceService = "svc",
            RuleType = null,
            Category = null,
            IconUrl = null,
            RuleConfig = "{}"
        };

        var created = new Achievement { Id = Guid.NewGuid(), Key = command.Key, Name = command.Name };
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        repo.AddAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>()).Returns(created);
        mapper.Map<CreateAchievementResponse>(created).Returns(new CreateAchievementResponse { Id = created.Id, Key = created.Key, Name = created.Name });

        var sut = new CreateAchievementCommandHandler(repo, mapper, validator, logger);
        var resp = await sut.Handle(command, CancellationToken.None);
        resp.Id.Should().Be(created.Id);
        resp.Key.Should().Be(created.Key);
        await repo.Received(1).AddAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>());
    }
}