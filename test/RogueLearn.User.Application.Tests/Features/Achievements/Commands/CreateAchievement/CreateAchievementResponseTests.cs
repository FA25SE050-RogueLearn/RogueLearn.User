using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.CreateAchievement;

public class CreateAchievementResponseTests
{
    [Fact]
    public void Properties_Set_And_Read()
    {
        var r = new CreateAchievementResponse
        {
            Id = Guid.NewGuid(),
            Key = "k",
            Name = "n",
            Description = "d",
            RuleType = "type",
            RuleConfig = "{}",
            Category = "cat",
            Icon = "i",
            IconUrl = "url",
            Version = 2,
            IsActive = true,
            SourceService = "svc"
        };

        r.Id.Should().NotBe(Guid.Empty);
        r.Key.Should().Be("k");
        r.Name.Should().Be("n");
        r.Description.Should().Be("d");
        r.RuleType.Should().Be("type");
        r.RuleConfig.Should().Be("{}");
        r.Category.Should().Be("cat");
        r.Icon.Should().Be("i");
        r.IconUrl.Should().Be("url");
        r.Version.Should().Be(2);
        r.IsActive.Should().BeTrue();
        r.SourceService.Should().Be("svc");
    }
}