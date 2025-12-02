using FluentAssertions;
using FluentValidation.TestHelper;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Tests.Features.SkillDependencies.Commands.AddSkillDependency;

public class AddSkillDependencyCommandValidatorTests
{
    [Fact]
    public void Should_Fail_When_SkillId_Empty()
    {
        var validator = new AddSkillDependencyCommandValidator();
        var cmd = new AddSkillDependencyCommand { SkillId = Guid.Empty, PrerequisiteSkillId = Guid.NewGuid() };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PrerequisiteSkillId_Empty()
    {
        var validator = new AddSkillDependencyCommandValidator();
        var cmd = new AddSkillDependencyCommand { SkillId = Guid.NewGuid(), PrerequisiteSkillId = Guid.Empty };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Self_Referencing()
    {
        var validator = new AddSkillDependencyCommandValidator();
        var id = Guid.NewGuid();
        var cmd = new AddSkillDependencyCommand { SkillId = id, PrerequisiteSkillId = id };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("A skill cannot depend on itself.");
    }

    [Fact]
    public void Should_Fail_When_RelationshipType_Invalid()
    {
        var validator = new AddSkillDependencyCommandValidator();
        var cmd = new AddSkillDependencyCommand { SkillId = Guid.NewGuid(), PrerequisiteSkillId = Guid.NewGuid(), RelationshipType = (SkillRelationshipType)999 };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("RelationshipType must be one of: Prerequisite, Corequisite, Recommended.");
    }

    [Fact]
    public void Should_Pass_With_Valid_Data()
    {
        var validator = new AddSkillDependencyCommandValidator();
        var cmd = new AddSkillDependencyCommand { SkillId = Guid.NewGuid(), PrerequisiteSkillId = Guid.NewGuid(), RelationshipType = SkillRelationshipType.Corequisite };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeTrue();
    }
}