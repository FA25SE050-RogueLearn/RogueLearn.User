using FluentAssertions;
using RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.CreateAchievement;

public class CreateAchievementCommandValidatorTests
{
    [Fact]
    public void Invalid_RuleConfig_Json_Fails()
    {
        var v = new CreateAchievementCommandValidator();
        var cmd = new CreateAchievementCommand
        {
            Key = "k",
            Name = "n",
            Description = "d",
            SourceService = "svc",
            RuleType = "threshold",
            RuleConfig = "{invalid}",
            Category = "core",
            Version = 1
        };
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_RuleConfig_Json_Passes()
    {
        var v = new CreateAchievementCommandValidator();
        var cmd = new CreateAchievementCommand
        {
            Key = "k",
            Name = "n",
            Description = "d",
            SourceService = "svc",
            RuleType = "threshold",
            RuleConfig = "{\"points\": 100}",
            Category = "core",
            Version = 1
        };
        v.Validate(cmd).IsValid.Should().BeTrue();
    }
}

