using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;
using RogueLearn.User.Domain.Enums;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;

public class UpdateCurriculumProgramResponseTests
{
    [Fact]
    public void Properties_Set_And_Read()
    {
        var r = new UpdateCurriculumProgramResponse
        {
            Id = Guid.NewGuid(),
            ProgramName = "P",
            ProgramCode = "PC",
            Description = "D",
            DegreeLevel = DegreeLevel.Master,
            TotalCredits = 180,
            DurationYears = 2,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        r.ProgramName.Should().Be("P");
        r.ProgramCode.Should().Be("PC");
        r.DegreeLevel.Should().Be(DegreeLevel.Master);
        r.TotalCredits.Should().Be(180);
        r.DurationYears.Should().Be(2);
    }
}