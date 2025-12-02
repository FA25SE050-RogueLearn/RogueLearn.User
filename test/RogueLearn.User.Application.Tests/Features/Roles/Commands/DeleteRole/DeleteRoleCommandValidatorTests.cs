using FluentAssertions;
using FluentValidation.TestHelper;
using RogueLearn.User.Application.Features.Roles.Commands.DeleteRole;

namespace RogueLearn.User.Application.Tests.Features.Roles.Commands.DeleteRole;

public class DeleteRoleCommandValidatorTests
{
    [Fact]
    public void Should_Fail_When_Id_Is_Empty()
    {
        var validator = new DeleteRoleCommandValidator();
        var cmd = new DeleteRoleCommand { Id = Guid.Empty };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Role ID is required.");
    }

    [Fact]
    public void Should_Pass_When_Id_Is_Provided()
    {
        var validator = new DeleteRoleCommandValidator();
        var cmd = new DeleteRoleCommand { Id = Guid.NewGuid() };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeTrue();
    }
}