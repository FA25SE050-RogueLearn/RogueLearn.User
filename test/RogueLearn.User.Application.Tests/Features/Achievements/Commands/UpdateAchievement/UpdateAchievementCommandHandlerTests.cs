using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.UpdateAchievement;

public class UpdateAchievementCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(UpdateAchievementCommand command)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new UpdateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<UpdateAchievementCommandHandler>>();

        repo.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Achievement?)null);

        command.Key = "key";
        command.Name = "name";
        command.Description = "desc";
        command.SourceService = "svc";
        command.RuleType = null;
        command.Category = null;
        command.IconUrl = null;
        command.RuleConfig = null;

        var sut = new UpdateAchievementCommandHandler(repo, mapper, validator, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_KeyConflictOnChange_Throws(UpdateAchievementCommand command)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new UpdateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<UpdateAchievementCommandHandler>>();

        var existing = new Achievement { Id = command.Id, Key = "old-key", Name = "n" };
        repo.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(existing);
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);

        command.Key = "new-key";
        command.Name = "name";
        command.Description = "desc";
        command.SourceService = "svc";
        command.RuleType = null;
        command.Category = null;
        command.IconUrl = null;
        command.RuleConfig = null;

        var sut = new UpdateAchievementCommandHandler(repo, mapper, validator, logger);
        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_InvalidRuleConfigArray_Throws(UpdateAchievementCommand command)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new UpdateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<UpdateAchievementCommandHandler>>();

        var existing = new Achievement { Id = command.Id, Key = command.Key, Name = "n" };
        repo.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(existing);
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);

        command.Key = "key";
        command.Name = "name";
        command.Description = "desc";
        command.SourceService = "svc";
        command.RuleType = null;
        command.Category = null;
        command.IconUrl = null;
        command.RuleConfig = "[]";

        var sut = new UpdateAchievementCommandHandler(repo, mapper, validator, logger);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ValidationException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_ReturnsResponse(UpdateAchievementCommand command)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new UpdateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<UpdateAchievementCommandHandler>>();

        var existing = new Achievement { Id = command.Id, Key = command.Key, Name = "n" };
        var updated = new Achievement { Id = command.Id, Key = command.Key, Name = command.Name };
        repo.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(existing);
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.UpdateAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>()).Returns(updated);
        mapper.Map<UpdateAchievementResponse>(updated).Returns(new UpdateAchievementResponse { Id = updated.Id, Key = updated.Key, Name = updated.Name, SourceService = command.SourceService });

        command.Key = "key";
        command.Name = "name";
        command.Description = "desc";
        command.SourceService = "svc";
        command.RuleType = null;
        command.Category = null;
        command.IconUrl = null;
        command.RuleConfig = "{}";

        var sut = new UpdateAchievementCommandHandler(repo, mapper, validator, logger);
        var resp = await sut.Handle(command, CancellationToken.None);
        resp.Id.Should().Be(updated.Id);
        resp.Key.Should().Be(updated.Key);
        await repo.Received(1).UpdateAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [AutoData]
    public async Task Handle_RuleConfigNull_SetsNullAndPersists(UpdateAchievementCommand command)
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var validator = new UpdateAchievementCommandValidator();
        var logger = Substitute.For<ILogger<UpdateAchievementCommandHandler>>();

        var existing = new Achievement { Id = command.Id, Key = command.Key, Name = "n" };
        var updated = new Achievement { Id = command.Id, Key = command.Key, Name = command.Name, Version = 1 };
        repo.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(existing);
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.UpdateAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Achievement>());
        mapper.Map<UpdateAchievementResponse>(Arg.Any<Achievement>()).Returns(new UpdateAchievementResponse { Id = updated.Id, Key = updated.Key, Name = updated.Name, Version = 1 });

        command.Key = existing.Key;
        command.Name = "name";
        command.Description = "desc";
        command.SourceService = "svc";
        command.Version = 1;
        command.RuleConfig = null;
        command.RuleType = null;
        command.Category = null;
        command.IconUrl = null;

        var sut = new UpdateAchievementCommandHandler(repo, mapper, validator, logger);
        var resp = await sut.Handle(command, CancellationToken.None);
        resp.Version.Should().Be(1);
        await repo.Received(1).UpdateAsync(Arg.Is<Achievement>(a => a.Version == 1 && a.RuleConfig == null), Arg.Any<CancellationToken>());
    }
}