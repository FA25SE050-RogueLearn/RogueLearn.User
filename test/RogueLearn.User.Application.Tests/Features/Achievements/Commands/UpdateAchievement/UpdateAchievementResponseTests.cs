using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.UpdateAchievement;

public class UpdateAchievementResponseTests
{
    [Fact]
    public void Properties_Set_And_Read()
    {
        var r = new UpdateAchievementResponse
        {
            Id = Guid.NewGuid(),
            Key = "k",
            Name = "n",
            Description = "d",
            RuleType = "type",
            RuleConfig = "{}",
            Category = "cat",
            IconUrl = "url",
            Version = 3,
            IsActive = false,
            SourceService = "svc"
        };

        r.Id.Should().NotBe(Guid.Empty);
        r.Key.Should().Be("k");
        r.Name.Should().Be("n");
        r.Description.Should().Be("d");
        r.RuleType.Should().Be("type");
        r.RuleConfig.Should().Be("{}");
        r.Category.Should().Be("cat");
        r.IconUrl.Should().Be("url");
        r.Version.Should().Be(3);
        r.IsActive.Should().BeFalse();
        r.SourceService.Should().Be("svc");
    }
}