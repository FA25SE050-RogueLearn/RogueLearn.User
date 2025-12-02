using FluentAssertions;
using FluentValidation.TestHelper;
using RogueLearn.User.Application.Features.Roles.Commands.CreateRole;

namespace RogueLearn.User.Application.Tests.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandValidatorTests
{
    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var validator = new CreateRoleCommandValidator();
        var cmd = new CreateRoleCommand { Name = "" };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Role name is required.");
    }

    [Fact]
    public void Should_Fail_When_Name_Exceeds_Max_Length()
    {
        var validator = new CreateRoleCommandValidator();
        var cmd = new CreateRoleCommand { Name = new string('a', 101) };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Role name cannot exceed 100 characters.");
    }

    [Fact]
    public void Should_Fail_When_Name_Has_Invalid_Characters()
    {
        var validator = new CreateRoleCommandValidator();
        var cmd = new CreateRoleCommand { Name = "Admin!" };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Role name can only contain letters, numbers, spaces, underscores, and hyphens.");
    }

    [Fact]
    public void Should_Pass_With_Valid_Name_And_Optional_Description()
    {
        var validator = new CreateRoleCommandValidator();
        var cmd = new CreateRoleCommand { Name = "Guild_Master-2", Description = null };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Description_Exceeds_Max_Length()
    {
        var validator = new CreateRoleCommandValidator();
        var cmd = new CreateRoleCommand { Name = "Member", Description = new string('b', 501) };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Role description cannot exceed 500 characters.");
    }
}