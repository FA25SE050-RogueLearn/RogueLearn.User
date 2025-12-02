using FluentAssertions;
using FluentValidation.TestHelper;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.CreateSubject;

public class CreateSubjectCommandValidatorTests
{
    [Fact]
    public void Should_Fail_When_SubjectCode_Empty()
    {
        var validator = new CreateSubjectCommandValidator();
        var cmd = new CreateSubjectCommand { SubjectCode = "", SubjectName = "Data Structures", Credits = 3 };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("SubjectCode is required.");
    }

    [Fact]
    public void Should_Fail_When_SubjectName_Empty()
    {
        var validator = new CreateSubjectCommandValidator();
        var cmd = new CreateSubjectCommand { SubjectCode = "CS201", SubjectName = "", Credits = 3 };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("SubjectName is required.");
    }

    [Fact]
    public void Should_Fail_When_Credits_Invalid()
    {
        var validator = new CreateSubjectCommandValidator();
        var cmd = new CreateSubjectCommand { SubjectCode = "CS201", SubjectName = "Data Structures", Credits = 0 };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Credits must be greater than 0.");
    }

    [Fact]
    public void Should_Pass_With_Valid_Data()
    {
        var validator = new CreateSubjectCommandValidator();
        var cmd = new CreateSubjectCommand { SubjectCode = "CS201", SubjectName = "Data Structures", Credits = 3, Description = new string('a', 100) };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeTrue();
    }
}