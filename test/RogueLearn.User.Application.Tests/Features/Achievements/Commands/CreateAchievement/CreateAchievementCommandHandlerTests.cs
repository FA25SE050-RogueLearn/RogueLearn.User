using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_KeyExists_ThrowsConflict(CreateAchievementCommand command)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new CreateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<CreateAchievementCommandHandler>>();

        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        command.Key = "key";
        command.Name = "name";
        command.Description = "desc";
        command.SourceService = "svc";
        command.RuleType = null;
        command.Category = null;
        command.IconUrl = null;
        command.RuleConfig = null;

        var sut = new CreateAchievementCommandHandler(repo, mapper, validator, logger);
        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_InvalidRuleConfigArray_ThrowsValidation(CreateAchievementCommand command)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new CreateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<CreateAchievementCommandHandler>>();

        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        command.Key = "key";
        command.Name = "name";
        command.Description = "desc";
        command.SourceService = "svc";
        command.RuleType = null;
        command.Category = null;
        command.IconUrl = null;
        command.RuleConfig = "[]";

        var sut = new CreateAchievementCommandHandler(repo, mapper, validator, logger);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ValidationException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_ReturnsResponse(CreateAchievementCommand command)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new CreateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<CreateAchievementCommandHandler>>();

        var created = new Achievement { Id = Guid.NewGuid(), Key = command.Key, Name = command.Name };
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        repo.AddAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>()).Returns(created);
        mapper.Map<CreateAchievementResponse>(created).Returns(new CreateAchievementResponse { Id = created.Id, Key = created.Key, Name = created.Name });

        command.Key = "key";
        command.Name = "name";
        command.Description = "desc";
        command.SourceService = "svc";
        command.RuleType = null;
        command.Category = null;
        command.IconUrl = null;
        command.RuleConfig = "{}";

        var sut = new CreateAchievementCommandHandler(repo, mapper, validator, logger);
        var resp = await sut.Handle(command, CancellationToken.None);
        resp.Id.Should().Be(created.Id);
        resp.Key.Should().Be(created.Key);
        await repo.Received(1).AddAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>());
    }
}