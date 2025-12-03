using System.Text.Json;
using FluentAssertions;
using RogueLearn.User.Application.DTOs.Internal;

namespace RogueLearn.User.Application.Tests.DTOs.Internal;

public class CurriculumForQuestGenerationDtoTests
{
    [Fact]
    public void JsonAttributesAreRespectedOnDeserialize()
    {
        var json = "{\"curriculumCode\":\"C\",\"name\":\"N\",\"subjects\":[{\"code\":\"S\",\"name\":\"SN\",\"semester\":1,\"credits\":3,\"prerequisites\":\"P\"}]}";
        var dto = JsonSerializer.Deserialize<CurriculumForQuestGenerationDto>(json)!;
        dto.CurriculumCode.Should().Be("C");
        dto.Name.Should().Be("N");
        dto.Subjects.Should().HaveCount(1);
        var s = dto.Subjects[0];
        s.Code.Should().Be("S");
        s.Name.Should().Be("SN");
        s.Semester.Should().Be(1);
        s.Credits.Should().Be(3);
        s.Prerequisites.Should().Be("P");
    }

    [Fact]
    public void JsonAttributesAreRespectedOnSerialize()
    {
        var dto = new CurriculumForQuestGenerationDto
        {
            CurriculumCode = "C",
            Name = "N",
            Subjects =
            {
                new SubjectForQuestGenerationDto { Code = "S", Name = "SN", Semester = 1, Credits = 3 }
            }
        };

        var json = JsonSerializer.Serialize(dto);
        json.Should().Contain("\"curriculumCode\":\"C\"");
        json.Should().Contain("\"name\":\"N\"");
        json.Should().Contain("\"subjects\"");
        json.Should().Contain("\"code\":\"S\"");
        json.Should().Contain("\"semester\":1");
        json.Should().Contain("\"credits\":3");
    }
}