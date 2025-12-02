using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.AddSubjectToProgram;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumProgramSubjects.Commands.AddSubjectToProgram;

public class AddSubjectToProgramRequestTests
{
    [Fact]
    public void Property_Set_And_Read()
    {
        var r = new AddSubjectToProgramRequest { SubjectId = Guid.NewGuid() };
        r.SubjectId.Should().NotBe(Guid.Empty);
    }
}