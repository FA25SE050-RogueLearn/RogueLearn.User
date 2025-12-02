using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;

namespace RogueLearn.User.Application.Tests.Features.UserRoles.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandValidatorTests
{
    [Fact]
    public async Task Valid_Passes()
    {
        var v = new RemoveRoleFromUserCommandValidator();
        var cmd = new RemoveRoleFromUserCommand { AuthUserId = System.Guid.NewGuid(), RoleId = System.Guid.NewGuid() };
        var res = await v.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_Fields_Fail()
    {
        var v = new RemoveRoleFromUserCommandValidator();
        var cmd = new RemoveRoleFromUserCommand { AuthUserId = default, RoleId = default };
        var res = await v.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveRoleFromUserCommand.AuthUserId));
        res.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveRoleFromUserCommand.RoleId));
    }
}