using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using AutoMapper;
using FluentValidation;
using RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.CreateAchievement;

public class CreateAchievementCommandHandlerTests
{
    private static CreateAchievementCommandHandler CreateSut(
        IAchievementRepository? repo = null,
        IMapper? mapper = null,
        IValidator<CreateAchievementCommand>? validator = null,
        Microsoft.Extensions.Logging.ILogger<CreateAchievementCommandHandler>? logger = null)
    {
        repo ??= Substitute.For<IAchievementRepository>();
        mapper ??= Substitute.For<IMapper>();
        validator ??= Substitute.For<IValidator<CreateAchievementCommand>>();
        logger ??= Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateAchievementCommandHandler>>();
        return new CreateAchievementCommandHandler(repo, mapper, validator, logger);
    }

    [Fact]
    public async Task Handle_KeyConflict_Throws()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<IMapper>();
        var validator = new CreateAchievementCommandValidator();
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut(repo, mapper, validator);
        var cmd = new CreateAchievementCommand { Key = "k", Name = "n", Description = "d", SourceService = "svc" };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.ConflictException>();
    }

    [Fact]
    public async Task Handle_RuleConfigArray_ThrowsValidationObject()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<IMapper>();
        var validator = new CreateAchievementCommandValidator();
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut(repo, mapper, validator);
        var cmd = new CreateAchievementCommand { Key = "k", Name = "n", Description = "d", SourceService = "svc", RuleConfig = "[]" };
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Handle_Success_CreatesEntityAndMapsResponse()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<IMapper>();
        var validator = new CreateAchievementCommandValidator();
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var created = new Achievement { Id = Guid.NewGuid(), Key = "k", Name = "n", Description = "d", SourceService = "svc" };
        repo.AddAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>())
            .Returns(created);
        mapper.Map<CreateAchievementResponse>(created).Returns(new CreateAchievementResponse { Id = created.Id, Key = created.Key, Name = created.Name, Description = created.Description, SourceService = created.SourceService });

        var sut = CreateSut(repo, mapper, validator);
        var cmd = new CreateAchievementCommand { Key = " k ", Name = " n ", Description = " d ", SourceService = " svc ", Version = 1 };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Id.Should().Be(created.Id);
        await repo.Received(1).AddAsync(Arg.Is<Achievement>(a => a.Key == "k" && a.Name == "n" && a.Description == "d" && a.SourceService == "svc"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VersionClampAndNullsHandled()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<IMapper>();
        var validator = new CreateAchievementCommandValidator();
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        repo.AddAsync(Arg.Any<Achievement>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Achievement>());
        mapper.Map<CreateAchievementResponse>(Arg.Any<Achievement>()).Returns(new CreateAchievementResponse());

        var sut = CreateSut(repo, mapper, validator);
        var cmd = new CreateAchievementCommand { Key = "x", Name = "y", Description = "z", SourceService = "svc", Version = 1, RuleType = null, RuleConfig = null, Category = null, IconUrl = null };
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).AddAsync(Arg.Is<Achievement>(a => a.Version == 1 && a.RuleType == null && a.RuleConfig == null && a.Category == null && a.IconUrl == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RuleConfigObject_ParsedAndAssigned()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<IMapper>();
        var validator = new CreateAchievementCommandValidator();
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Achievement? captured = null;
        repo.AddAsync(Arg.Do<Achievement>(a => captured = a), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Achievement>());
        mapper.Map<CreateAchievementResponse>(Arg.Any<Achievement>()).Returns(new CreateAchievementResponse());

        var sut = CreateSut(repo, mapper, validator);
        var cmd = new CreateAchievementCommand { Key = "k", Name = "n", Description = "d", SourceService = "svc", RuleConfig = "{\"points\":10,\"enabled\":true}" };
        await sut.Handle(cmd, CancellationToken.None);

        captured!.RuleConfig.Should().NotBeNull();
        captured!.RuleConfig!.Should().ContainKey("points");
        captured!.RuleConfig!.Should().ContainKey("enabled");
    }

    [Fact]
    public async Task Handle_RuleConfigWhitespace_SkipsParsing()
    {
        var repo = Substitute.For<IAchievementRepository>();
        var mapper = Substitute.For<IMapper>();
        var validator = new CreateAchievementCommandValidator();
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Achievement, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Achievement? captured = null;
        repo.AddAsync(Arg.Do<Achievement>(a => captured = a), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Achievement>());
        mapper.Map<CreateAchievementResponse>(Arg.Any<Achievement>()).Returns(new CreateAchievementResponse());

        var sut = CreateSut(repo, mapper, validator);
        var cmd = new CreateAchievementCommand { Key = "k", Name = "n", Description = "d", SourceService = "svc", RuleConfig = "   " };
        await sut.Handle(cmd, CancellationToken.None);

        captured!.RuleConfig.Should().BeNull();
    }
}
