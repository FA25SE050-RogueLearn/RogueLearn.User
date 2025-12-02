using FluentAssertions;
using FluentValidation.TestHelper;
using RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;

namespace RogueLearn.User.Application.Tests.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandValidatorTests
{
    [Fact]
    public void Should_Fail_When_Id_Is_Empty()
    {
        var validator = new UpdateRoleCommandValidator();
        var cmd = new UpdateRoleCommand { Id = Guid.Empty, Name = "Member" };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Role ID is required.");
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Invalid()
    {
        var validator = new UpdateRoleCommandValidator();
        var cmd = new UpdateRoleCommand { Id = Guid.NewGuid(), Name = "Admin!" };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Role name can only contain letters, numbers, spaces, underscores, and hyphens.");
    }

    [Fact]
    public void Should_Pass_With_Valid_Id_And_Name()
    {
        var validator = new UpdateRoleCommandValidator();
        var cmd = new UpdateRoleCommand { Id = Guid.NewGuid(), Name = "Guild_Master-2" };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Description_Too_Long()
    {
        var validator = new UpdateRoleCommandValidator();
        var cmd = new UpdateRoleCommand { Id = Guid.NewGuid(), Name = "Member", Description = new string('x', 501) };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Role description cannot exceed 500 characters.");
    }
}