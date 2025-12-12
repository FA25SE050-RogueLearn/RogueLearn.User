using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;
using FluentAssertions;

namespace RogueLearn.User.Application.Tests.Features.CurriculumImport.Commands.ImportCurriculum;

public class ImportCurriculumResponseTests
{
    [Fact]
    public void Map_ImportCurriculumResponse_Fields_Are_Mapped()
    {
        var response = new ImportCurriculumResponse
        (
            
        )
        {
            IsSuccess = true,
            CurriculumProgramId = Guid.NewGuid(),
            CurriculumVersionId = Guid.NewGuid(),
            Message = "Import curriculum successfully",
            SubjectIds = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
            }
        };
        response.IsSuccess.Should().BeTrue();
        response.CurriculumProgramId.Should().NotBeEmpty();
        response.CurriculumVersionId.Should().NotBeEmpty();
        response.Message.Should().NotBeEmpty();
        response.SubjectIds.Should().NotBeEmpty();
    }
}