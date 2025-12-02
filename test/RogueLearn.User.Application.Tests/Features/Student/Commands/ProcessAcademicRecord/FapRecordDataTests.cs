using System.Text.Json;
using FluentAssertions;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

namespace RogueLearn.User.Application.Tests.Features.Student.Commands.ProcessAcademicRecord;

public class FapRecordDataTests
{
    [Fact]
    public void FapRecordData_SerializeDeserialize_Works()
    {
        var data = new FapRecordData
        {
            Gpa = 3.2,
            Subjects =
            {
                new FapSubjectData
                {
                    SubjectCode = "PRO101",
                    SubjectName = "Programming",
                    Status = "Passed",
                    Mark = 8.5,
                    Semester = 1,
                    AcademicYear = "2023"
                }
            }
        };

        var json = JsonSerializer.Serialize(data);
        var back = JsonSerializer.Deserialize<FapRecordData>(json);
        back!.Gpa.Should().Be(3.2);
        back.Subjects.Count.Should().Be(1);
        back.Subjects[0].SubjectCode.Should().Be("PRO101");
        back.Subjects[0].Semester.Should().Be(1);
    }
}