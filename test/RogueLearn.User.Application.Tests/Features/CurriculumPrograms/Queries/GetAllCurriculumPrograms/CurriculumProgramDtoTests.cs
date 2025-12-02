using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;
using RogueLearn.User.Domain.Enums;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;

public class CurriculumProgramDtoTests
{
    [Fact]
    public void Properties_Set_And_Read()
    {
        var d = new CurriculumProgramDto
        {
            Id = Guid.NewGuid(),
            ProgramName = "P",
            ProgramCode = "PC",
            Description = "D",
            DegreeLevel = DegreeLevel.Bachelor,
            TotalCredits = 120,
            DurationYears = 4,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        d.ProgramName.Should().Be("P");
        d.ProgramCode.Should().Be("PC");
        d.DegreeLevel.Should().Be(DegreeLevel.Bachelor);
        d.TotalCredits.Should().Be(120);
        d.DurationYears.Should().Be(4);
    }
}