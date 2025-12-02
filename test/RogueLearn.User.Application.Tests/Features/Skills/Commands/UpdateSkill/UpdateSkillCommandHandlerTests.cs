using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Skills.Commands.UpdateSkill;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Skills.Commands.UpdateSkill;

public class UpdateSkillCommandHandlerTests
{
    private static UpdateSkillCommandHandler CreateSut(ISkillRepository? repo = null, ILogger<UpdateSkillCommandHandler>? logger = null)
    {
        repo ??= Substitute.For<ISkillRepository>();
        logger ??= Substitute.For<ILogger<UpdateSkillCommandHandler>>();
        return new UpdateSkillCommandHandler(repo, logger);
    }

    [Fact]
    public async Task Handle_ThrowsNotFound_When_Missing()
    {
        var repo = Substitute.For<ISkillRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Skill?)null);
        var sut = CreateSut(repo);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new UpdateSkillCommand { Id = Guid.NewGuid(), Name = "N" }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Updates_And_ReturnsResponse()
    {
        var existing = new Skill { Id = Guid.NewGuid(), Name = "Old", Domain = "D", Tier = SkillTierLevel.Foundation, Description = "X" };
        var repo = Substitute.For<ISkillRepository>();
        repo.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        repo.UpdateAsync(Arg.Any<Skill>(), Arg.Any<CancellationToken>()).Returns(ci => (Skill)ci[0]!);
        var sut = CreateSut(repo);
        var cmd = new UpdateSkillCommand { Id = existing.Id, Name = "New", Domain = "ND", Tier = (int)SkillTierLevel.Advanced, Description = "Y" };
        var res = await sut.Handle(cmd, CancellationToken.None);
        res.Name.Should().Be("New");
        res.Domain.Should().Be("ND");
        res.Tier.Should().Be((int)SkillTierLevel.Advanced);
        res.Description.Should().Be("Y");
        await repo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }
}