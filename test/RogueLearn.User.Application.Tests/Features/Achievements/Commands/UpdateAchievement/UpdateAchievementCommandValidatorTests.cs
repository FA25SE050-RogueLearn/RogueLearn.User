using FluentAssertions;
using RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;

namespace RogueLearn.User.Application.Tests.Features.Achievements.Commands.UpdateAchievement;

public class UpdateAchievementCommandValidatorTests
{
    [Fact]
    public void Invalid_RuleConfig_Json_Fails()
    {
        var v = new UpdateAchievementCommandValidator();
        var cmd = new UpdateAchievementCommand
        {
            Id = Guid.NewGuid(),
            Key = "k",
            Name = "n",
            Description = "d",
            SourceService = "svc",
            RuleType = "streak",
            RuleConfig = "{invalid}",
            Category = "core",
            Version = 1
        };
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_RuleConfig_Json_Passes()
    {
        var v = new UpdateAchievementCommandValidator();
        var cmd = new UpdateAchievementCommand
        {
            Id = Guid.NewGuid(),
            Key = "k",
            Name = "n",
            Description = "d",
            SourceService = "svc",
            RuleType = "streak",
            RuleConfig = "{\"days\": 7}",
            Category = "core",
            Version = 1
        };
        v.Validate(cmd).IsValid.Should().BeTrue();
    }
}

