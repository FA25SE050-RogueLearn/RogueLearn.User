using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;

namespace RogueLearn.User.Application.Tests.Features.UserRoles.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandValidatorTests
{
    [Fact]
    public async Task Valid_Passes()
    {
        var v = new AssignRoleToUserCommandValidator();
        var cmd = new AssignRoleToUserCommand { AuthUserId = System.Guid.NewGuid(), RoleId = System.Guid.NewGuid() };
        var res = await v.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_Fields_Fail()
    {
        var v = new AssignRoleToUserCommandValidator();
        var cmd = new AssignRoleToUserCommand { AuthUserId = default, RoleId = default };
        var res = await v.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(AssignRoleToUserCommand.AuthUserId));
        res.Errors.Should().Contain(e => e.PropertyName == nameof(AssignRoleToUserCommand.RoleId));
    }
}