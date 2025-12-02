using System.Text.Json;
using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class AcademicRecordModelsTests
{
    [Fact]
    public void FapRecordData_Can_Serialize_And_Deserialize()
    {
        var data = new FapRecordData
        {
            Gpa = 3.8,
            TotalCredits = 120,
            Subjects =
            {
                new FapSubjectData { SubjectCode = "CS101", Status = "Passed", Mark = 8.5, Credits = 3 }
            }
        };
        var json = JsonSerializer.Serialize(data);
        var back = JsonSerializer.Deserialize<FapRecordData>(json);
        back!.Gpa.Should().Be(3.8);
        back.Subjects.Count.Should().Be(1);
        back.Subjects[0].SubjectCode.Should().Be("CS101");
    }
}