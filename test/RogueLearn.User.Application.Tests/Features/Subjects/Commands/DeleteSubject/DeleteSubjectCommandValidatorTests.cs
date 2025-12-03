using FluentAssertions;
using FluentValidation.TestHelper;
using RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.DeleteSubject;

public class DeleteSubjectCommandValidatorTests
{
    [Fact]
    public void Should_Fail_When_Id_Empty()
    {
        var validator = new DeleteSubjectCommandValidator();
        var cmd = new DeleteSubjectCommand { Id = Guid.Empty };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Id is required.");
    }

    [Fact]
    public void Should_Pass_When_Id_Present()
    {
        var validator = new DeleteSubjectCommandValidator();
        var cmd = new DeleteSubjectCommand { Id = Guid.NewGuid() };
        var result = validator.TestValidate(cmd);
        result.IsValid.Should().BeTrue();
    }
}