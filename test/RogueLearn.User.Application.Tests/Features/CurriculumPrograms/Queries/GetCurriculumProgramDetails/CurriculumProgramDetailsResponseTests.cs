using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Tests.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

public class CurriculumProgramDetailsResponseTests
{
    [Fact]
    public void CurriculumProgramDetailsResponse_SetsFields()
    {
        var dto = new CurriculumProgramDetailsResponse
        {
            Id = Guid.NewGuid(),
            ProgramName = "n",
            ProgramCode = "c",
            Description = "d",
            DegreeLevel = DegreeLevel.Bachelor,
            TotalCredits = 120,
            DurationYears = 4,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Analysis = new CurriculumAnalysisDto
            {
                ContentCompletionPercentage = 80,
                MissingContentSubjects =
                [
                    "Math",
                    "Physics",
                ],
                SubjectsWithContent = 10,
                SubjectsWithoutContent = 1,
                TotalSubjects = 8,
                TotalVersions = 1,
            },
            CurriculumVersions = new List<CurriculumVersionDetailsDto>
            {
                new CurriculumVersionDetailsDto
                {
                    Id = Guid.NewGuid(),
                    VersionCode = "v1",
                    Description = "d",
                    Analysis = new CurriculumVersionAnalysisDto
                    {
                        ContentCompletionPercentage = 80,
                        SubjectsWithContent = 10,
                        SubjectsWithoutContent = 1,
                        TotalSubjects = 8,
                    },
                    CreatedAt = DateTimeOffset.UtcNow,
                    EffectiveYear = 2024,
                    IsActive = true,
                    Subjects = []
                },
            },
        };
        dto.ProgramName.Should().Be("n");
        dto.ProgramCode.Should().Be("c");
        dto.Description.Should().Be("d");
    }
}