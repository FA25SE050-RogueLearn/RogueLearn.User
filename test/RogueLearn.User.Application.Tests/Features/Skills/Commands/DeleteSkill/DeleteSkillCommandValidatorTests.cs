using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RogueLearn.User.Application.Features.Skills.Commands.DeleteSkill;

namespace RogueLearn.User.Application.Tests.Features.Skills.Commands.DeleteSkill;

public class DeleteSkillCommandValidatorTests
{
    [Fact]
    public async Task Valid_Id_Passes()
    {
        var validator = new DeleteSkillCommandValidator();
        var cmd = new DeleteSkillCommand { Id = System.Guid.NewGuid() };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_Id_Fails()
    {
        var validator = new DeleteSkillCommandValidator();
        var cmd = new DeleteSkillCommand { Id = default };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(DeleteSkillCommand.Id));
    }
}