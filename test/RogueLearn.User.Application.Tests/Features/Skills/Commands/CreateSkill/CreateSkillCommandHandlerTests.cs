using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Skills.Commands.CreateSkill;

public class CreateSkillCommandHandlerTests
{
    private static CreateSkillCommandHandler CreateSut(ISkillRepository? repo = null, ILogger<CreateSkillCommandHandler>? logger = null)
    {
        repo ??= Substitute.For<ISkillRepository>();
        logger ??= Substitute.For<ILogger<CreateSkillCommandHandler>>();
        return new CreateSkillCommandHandler(repo, logger);
    }

    [Fact]
    public async Task Handle_ThrowsConflict_When_NameExists()
    {
        var repo = Substitute.For<ISkillRepository>();
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Skill, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateSut(repo);
        var cmd = new CreateSkillCommand { Name = "Algo", Tier = (int)SkillTierLevel.Foundation };
        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CreatesSkill_And_ReturnsResponse()
    {
        var repo = Substitute.For<ISkillRepository>();
        repo.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Skill, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.AddAsync(Arg.Any<Skill>(), Arg.Any<CancellationToken>()).Returns(ci => (Skill)ci[0]!);
        var sut = CreateSut(repo);
        var cmd = new CreateSkillCommand { Name = "DS", Domain = "CS", Tier = (int)SkillTierLevel.Intermediate, Description = "Desc" };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Name.Should().Be("DS");
        res.Domain.Should().Be("CS");
        res.Tier.Should().Be((int)SkillTierLevel.Intermediate);
        res.Description.Should().Be("Desc");
    }
}