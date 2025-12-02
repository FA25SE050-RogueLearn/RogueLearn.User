using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RogueLearn.User.Application.Features.Skills.Commands.UpdateSkill;

namespace RogueLearn.User.Application.Tests.Features.Skills.Commands.UpdateSkill;

public class UpdateSkillCommandValidatorTests
{
    [Fact]
    public async Task Valid_Minimal_Passes()
    {
        var validator = new UpdateSkillCommandValidator();
        var cmd = new UpdateSkillCommand { Id = System.Guid.NewGuid(), Name = "Skill", Tier = 1 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_Id_Fails()
    {
        var validator = new UpdateSkillCommandValidator();
        var cmd = new UpdateSkillCommand { Id = default, Name = "Skill", Tier = 1 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSkillCommand.Id));
    }

    [Fact]
    public async Task Empty_Name_Fails()
    {
        var validator = new UpdateSkillCommandValidator();
        var cmd = new UpdateSkillCommand { Id = System.Guid.NewGuid(), Name = string.Empty, Tier = 1 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSkillCommand.Name));
    }

    [Fact]
    public async Task Tier_Less_Than_One_Fails()
    {
        var validator = new UpdateSkillCommandValidator();
        var cmd = new UpdateSkillCommand { Id = System.Guid.NewGuid(), Name = "Skill", Tier = 0 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSkillCommand.Tier));
    }

    [Fact]
    public async Task Domain_Too_Long_Fails_When_Provided()
    {
        var validator = new UpdateSkillCommandValidator();
        var longDomain = new string('a', 256);
        var cmd = new UpdateSkillCommand { Id = System.Guid.NewGuid(), Name = "Skill", Tier = 1, Domain = longDomain };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSkillCommand.Domain));
    }

    [Fact]
    public async Task Description_Too_Long_Fails_When_Provided()
    {
        var validator = new UpdateSkillCommandValidator();
        var longDesc = new string('a', 2001);
        var cmd = new UpdateSkillCommand { Id = System.Guid.NewGuid(), Name = "Skill", Tier = 1, Description = longDesc };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSkillCommand.Description));
    }
}