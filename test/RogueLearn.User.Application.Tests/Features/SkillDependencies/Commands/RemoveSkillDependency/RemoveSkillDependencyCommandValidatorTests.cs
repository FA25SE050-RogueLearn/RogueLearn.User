using FluentAssertions;
using FluentValidation.TestHelper;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.RemoveSkillDependency;

namespace RogueLearn.User.Application.Tests.Features.SkillDependencies.Commands.RemoveSkillDependency;

public class RemoveSkillDependencyCommandValidatorTests
{
    [Fact]
    public void Should_Fail_When_SkillId_Empty()
    {
        var validator = new RemoveSkillDependencyCommandValidator();
        var cmd = new RemoveSkillDependencyCommand { SkillId = Guid.Empty, PrerequisiteSkillId = Guid.NewGuid() };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PrerequisiteSkillId_Empty()
    {
        var validator = new RemoveSkillDependencyCommandValidator();
        var cmd = new RemoveSkillDependencyCommand { SkillId = Guid.NewGuid(), PrerequisiteSkillId = Guid.Empty };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Self_Referencing()
    {
        var validator = new RemoveSkillDependencyCommandValidator();
        var id = Guid.NewGuid();
        var cmd = new RemoveSkillDependencyCommand { SkillId = id, PrerequisiteSkillId = id };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("A skill cannot depend on itself.");
    }

    [Fact]
    public void Should_Pass_With_Valid_Data()
    {
        var validator = new RemoveSkillDependencyCommandValidator();
        var cmd = new RemoveSkillDependencyCommand { SkillId = Guid.NewGuid(), PrerequisiteSkillId = Guid.NewGuid() };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeTrue();
    }
}