using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.UpdateSubject;

public class UpdateSubjectCommandValidatorTests
{
    [Fact]
    public async Task Valid_Minimal_Passes()
    {
        var validator = new UpdateSubjectCommandValidator();
        var cmd = new UpdateSubjectCommand { Id = System.Guid.NewGuid(), SubjectCode = "CS101", SubjectName = "Intro", Credits = 3 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_Fields_Fail()
    {
        var validator = new UpdateSubjectCommandValidator();
        var cmd = new UpdateSubjectCommand { Id = default, SubjectCode = string.Empty, SubjectName = string.Empty, Credits = 0 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSubjectCommand.Id));
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSubjectCommand.SubjectCode));
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSubjectCommand.SubjectName));
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSubjectCommand.Credits));
    }

    [Fact]
    public async Task SubjectCode_Name_Length_Too_Long_Fail()
    {
        var validator = new UpdateSubjectCommandValidator();
        var longCode = new string('C', 51);
        var longName = new string('N', 201);
        var cmd = new UpdateSubjectCommand { Id = System.Guid.NewGuid(), SubjectCode = longCode, SubjectName = longName, Credits = 3 };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSubjectCommand.SubjectCode));
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSubjectCommand.SubjectName));
    }

    [Fact]
    public async Task Description_Too_Long_Fails()
    {
        var validator = new UpdateSubjectCommandValidator();
        var longDesc = new string('d', 1001);
        var cmd = new UpdateSubjectCommand { Id = System.Guid.NewGuid(), SubjectCode = "CS101", SubjectName = "Intro", Credits = 3, Description = longDesc };
        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSubjectCommand.Description));
    }
}