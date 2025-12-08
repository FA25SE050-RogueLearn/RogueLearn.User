using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.Achievements.Queries.GetAllAchievements;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Queries.GetAllAchievements;

public class AchievementDtoTests
{
    [Fact]
    public void Properties_Set_And_Read()
    {
        var d = new AchievementDto
        {
            Id = Guid.NewGuid(),
            Key = "k",
            Name = "n",
            Description = "d",
            RuleType = "rt",
            RuleConfig = "{}",
            Category = "c",
            IconUrl = "url",
            Version = 1,
            IsActive = true,
            SourceService = "svc",
            IsMedal = true
        };

        d.Id.Should().NotBe(Guid.Empty);
        d.Key.Should().Be("k");
        d.Name.Should().Be("n");
        d.Description.Should().Be("d");
        d.RuleType.Should().Be("rt");
        d.RuleConfig.Should().Be("{}");
        d.Category.Should().Be("c");
        d.IconUrl.Should().Be("url");
        d.Version.Should().Be(1);
        d.IsActive.Should().BeTrue();
        d.SourceService.Should().Be("svc");
        d.IsMedal.Should().BeTrue();
    }
}