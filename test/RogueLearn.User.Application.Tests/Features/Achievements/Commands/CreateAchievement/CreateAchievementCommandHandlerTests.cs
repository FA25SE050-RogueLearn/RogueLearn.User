using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentValidation;
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
        var mapper = Substitute.For<IMapper>();
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
    public async Task Handle_InvalidRuleConfig_ThrowsValidation()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<IMapper>();
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
            RuleConfig = "{invalid-json}"
        };

        var sut = new CreateAchievementCommandHandler(repo, mapper, validator, logger);
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ReturnsResponse()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<IMapper>();
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
            RuleConfig = "{\"points\":10}"
        };

        repo.AddAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>()).Returns(ci => (Achievement)ci[0]!);

        mapper.Map<CreateAchievementResponse>(Arg.Any<Achievement>()).Returns(ci =>
        {
            var a = (Achievement)ci[0]!;
            return new CreateAchievementResponse { Id = a.Id, Key = a.Key, Name = a.Name };
        });

        var sut = new CreateAchievementCommandHandler(repo, mapper, validator, logger);
        var resp = await sut.Handle(command, CancellationToken.None);

        resp.Key.Should().Be("key");
        resp.Name.Should().Be("name");
        await repo.Received(1).AddAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidRuleConfigJson_ThrowsValidation()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<IMapper>();
        var validator = new CreateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<CreateAchievementCommandHandler>>();

        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = new CreateAchievementCommandHandler(repo, mapper, validator, logger);
        var cmd = new CreateAchievementCommand { Key = "k", Name = "n", Description = "d", SourceService = "svc", RuleConfig = "{invalid" };

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(act);
    }
}
