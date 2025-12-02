using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.CreateCurriculumProgram;
using RogueLearn.User.Domain.Enums;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Commands.CreateCurriculumProgram;

public class CreateCurriculumProgramResponseTests
{
    [Fact]
    public void Properties_Set_And_Read()
    {
        var r = new CreateCurriculumProgramResponse
        {
            Id = Guid.NewGuid(),
            ProgramName = "P",
            ProgramCode = "PC",
            Description = "D",
            DegreeLevel = DegreeLevel.Bachelor,
            TotalCredits = 120,
            DurationYears = 4,
            CreatedAt = DateTimeOffset.UtcNow
        };
        r.ProgramName.Should().Be("P");
        r.ProgramCode.Should().Be("PC");
        r.DegreeLevel.Should().Be(DegreeLevel.Bachelor);
        r.TotalCredits.Should().Be(120);
        r.DurationYears.Should().Be(4);
    }
}