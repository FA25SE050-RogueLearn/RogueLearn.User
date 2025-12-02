using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Skills.Queries.GetSkillById;

public class GetSkillByIdQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(Guid skillId)
    {
        var repo = Substitute.For<ISkillRepository>();
        var logger = Substitute.For<ILogger<GetSkillByIdQueryHandler>>();
        repo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns((Skill?)null);

        var sut = new GetSkillByIdQueryHandler(repo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new GetSkillByIdQuery { Id = skillId }, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Found_MapsResponse(Guid skillId)
    {
        var repo = Substitute.For<ISkillRepository>();
        var logger = Substitute.For<ILogger<GetSkillByIdQueryHandler>>();
        var skill = new Skill { Id = skillId, Name = "C#", Domain = "Programming", Tier = SkillTierLevel.Advanced, Description = "Deep" };
        repo.GetByIdAsync(skill.Id, Arg.Any<CancellationToken>()).Returns(skill);

        var sut = new GetSkillByIdQueryHandler(repo, logger);
        var result = await sut.Handle(new GetSkillByIdQuery { Id = skill.Id }, CancellationToken.None);

        result.Id.Should().Be(skill.Id);
        result.Name.Should().Be("C#");
        result.Tier.Should().Be((int)SkillTierLevel.Advanced);
    }
}