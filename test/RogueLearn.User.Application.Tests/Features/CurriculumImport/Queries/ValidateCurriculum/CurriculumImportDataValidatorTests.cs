using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Models;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumImport.Queries.ValidateCurriculum;

public class CurriculumImportDataValidatorTests
{
    [Fact]
    public void ValidData_Passes()
    {
        var schema = new CurriculumImportData
        {
            Program = new CurriculumProgramData { ProgramName = "PN", ProgramCode = "PC", Description = "D", DegreeLevel = RogueLearn.User.Domain.Enums.DegreeLevel.Bachelor, TotalCredits = 120, DurationYears = 4 },
            Version = new CurriculumVersionData { VersionCode = "V1", EffectiveYear = 2024 },
            Subjects = new() { new SubjectData { SubjectCode = "S1", SubjectName = "SN1", Credits = 3 } },
            Structure = new() { new CurriculumStructureData { SubjectCode = "S1", TermNumber = 1, IsMandatory = true } }
        };

        var validator = new CurriculumImportDataValidator();
        var result = validator.Validate(schema);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvalidProgramName_Fails()
    {
        var schema = new CurriculumImportData
        {
            Program = new CurriculumProgramData { ProgramName = "", ProgramCode = "PC", DegreeLevel = RogueLearn.User.Domain.Enums.DegreeLevel.Bachelor },
            Version = new CurriculumVersionData { VersionCode = "V1", EffectiveYear = 2024 },
            Subjects = new() { new SubjectData { SubjectCode = "S1", SubjectName = "SN1", Credits = 3 } },
            Structure = new() { new CurriculumStructureData { SubjectCode = "S1", TermNumber = 1, IsMandatory = true } }
        };

        var validator = new CurriculumImportDataValidator();
        var result = validator.Validate(schema);
        result.IsValid.Should().BeFalse();
    }
}