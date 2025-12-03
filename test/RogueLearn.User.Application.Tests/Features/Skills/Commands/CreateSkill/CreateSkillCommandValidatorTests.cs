using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;

namespace RogueLearn.User.Application.Tests.Features.Skills.Commands.CreateSkill;

public class CreateSkillCommandValidatorTests
{
    [Fact]
    public async Task Valid_Minimal_Passes()
    {
        var validator = new CreateSkillCommandValidator();
        var cmd = new CreateSkillCommand { Name = "Skill", Tier = 1 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_Name_Fails()
    {
        var validator = new CreateSkillCommandValidator();
        var cmd = new CreateSkillCommand { Name = string.Empty, Tier = 1 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSkillCommand.Name));
    }

    [Fact]
    public async Task Tier_Less_Than_One_Fails()
    {
        var validator = new CreateSkillCommandValidator();
        var cmd = new CreateSkillCommand { Name = "Skill", Tier = 0 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSkillCommand.Tier));
    }

    [Fact]
    public async Task Domain_Too_Long_Fails_When_Provided()
    {
        var validator = new CreateSkillCommandValidator();
        var longDomain = new string('a', 256);
        var cmd = new CreateSkillCommand { Name = "Skill", Tier = 1, Domain = longDomain };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSkillCommand.Domain));
    }

    [Fact]
    public async Task Description_Too_Long_Fails_When_Provided()
    {
        var validator = new CreateSkillCommandValidator();
        var longDesc = new string('a', 2001);
        var cmd = new CreateSkillCommand { Name = "Skill", Tier = 1, Description = longDesc };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSkillCommand.Description));
    }
}