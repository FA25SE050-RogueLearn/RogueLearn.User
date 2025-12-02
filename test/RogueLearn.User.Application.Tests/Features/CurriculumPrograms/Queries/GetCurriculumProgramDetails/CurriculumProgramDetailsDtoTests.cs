using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

public class CurriculumProgramDetailsDtoTests
{
    [Fact]
    public void Details_Response_Set_And_Read()
    {
        var r = new CurriculumProgramDetailsResponse
        {
            Id = Guid.NewGuid(),
            ProgramName = "P",
            ProgramCode = "PC",
            TotalCredits = 120,
            DurationYears = 4
        };
        r.ProgramName.Should().Be("P");
        r.ProgramCode.Should().Be("PC");
    }

    [Fact]
    public void Version_Dto_Set_And_Read()
    {
        var v = new CurriculumVersionDetailsDto
        {
            Id = Guid.NewGuid(),
            VersionCode = "v1",
            EffectiveYear = 2024,
            IsActive = true,
            Description = "desc",
            CreatedAt = DateTimeOffset.UtcNow
        };
        v.VersionCode.Should().Be("v1");
        v.EffectiveYear.Should().Be(2024);
        v.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Subject_Dto_Set_And_Read()
    {
        var s = new CurriculumSubjectDetailsDto
        {
            SubjectId = Guid.NewGuid(),
            SubjectCode = "SC",
            SubjectName = "SN",
            Credits = 3,
            IsMandatory = true,
            TermNumber = 1,
            Analysis = new SubjectAnalysisDto { HasContentInLatestVersion = true, Status = "Complete" }
        };
        s.SubjectCode.Should().Be("SC");
        s.Credits.Should().Be(3);
        s.IsMandatory.Should().BeTrue();
        s.TermNumber.Should().Be(1);
        s.Analysis.Status.Should().Be("Complete");
    }

    [Fact]
    public void Analysis_Dtos_Set_And_Read()
    {
        var ca = new CurriculumAnalysisDto { TotalVersions = 2, TotalSubjects = 10, ContentCompletionPercentage = 50 };
        var va = new CurriculumVersionAnalysisDto { TotalSubjects = 5, MandatorySubjects = 4, ElectiveSubjects = 1 };
        ca.TotalVersions.Should().Be(2);
        va.MandatorySubjects.Should().Be(4);
    }
}