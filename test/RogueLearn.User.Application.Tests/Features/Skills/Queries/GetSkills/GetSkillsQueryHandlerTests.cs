using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkills;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Skills.Queries.GetSkills;

public class GetSkillsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedSkills()
    {
        var repo = Substitute.For<ISkillRepository>();
        var logger = Substitute.For<ILogger<GetSkillsQueryHandler>>();
        var skills = new List<Skill>
        {
            new() { Id = Guid.NewGuid(), Name = "C#", Domain = "Programming", Tier = SkillTierLevel.Foundation, Description = "Basics" },
            new() { Id = Guid.NewGuid(), Name = "ASP.NET", Domain = "Web", Tier = SkillTierLevel.Intermediate, Description = "Web dev" }
        };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(skills);

        var sut = new GetSkillsQueryHandler(repo, logger);
        var result = await sut.Handle(new GetSkillsQuery(), CancellationToken.None);

        result.Skills.Should().HaveCount(2);
        result.Skills.Select(s => s.Name).Should().Contain(new[] { "C#", "ASP.NET" });
        result.Skills.First().Tier.Should().Be((int)SkillTierLevel.Foundation);
    }
}