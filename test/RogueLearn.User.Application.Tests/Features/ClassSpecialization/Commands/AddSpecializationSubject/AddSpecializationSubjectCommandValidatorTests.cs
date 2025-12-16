using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.ClassSpecialization.Commands.AddSpecializationSubject;

public class AddSpecializationSubjectCommandValidatorTests
{
    [Fact]
    public void Validate_Passes_For_Valid()
    {
        var v = new AddSpecializationSubjectCommandValidator();
        var cmd = new AddSpecializationSubjectCommand
        {
            ClassId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            Semester = 1
     
        };
        var result = v.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_For_Missing_Class_Or_Subject()
    {
        var v = new AddSpecializationSubjectCommandValidator();
        var cmd = new AddSpecializationSubjectCommand
        {
            ClassId = Guid.Empty,
            SubjectId = Guid.Empty,
            Semester = 0
        };
        var result = v.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}